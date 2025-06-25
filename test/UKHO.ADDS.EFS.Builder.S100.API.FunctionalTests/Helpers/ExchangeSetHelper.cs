using System.Text.Json;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers
{
    public class ExchangeSetHelper
    {
        private static readonly List<string> builderNodeNames = new()
           {
               "ReadConfigurationNode",
               "StartTomcatNode",
               "CheckEndpointsNode",
               "GetJobNode",               
               "ProductSearchNode",
               "DownloadFilesNode",
               "AddExchangeSetNode",
               "AddContentExchangeSetNode",
               "SignExchangeSetNode",
               "ExtractExchangeSetNode",
               "UploadFilesNode"
           };

        public static async Task WaitForExchangeSetGeneration(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();

            bool reached = await tableHelpers.WaitForEntityCountAsync(partitionKey, builderNodeNames.Count);
            Assert.That(reached, Is.True, "Exchange Set Generation failed as all the Builder nodes didn't succeed ");
        }

        public static async Task verifyAllBuilderNodesSucceeded2(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();
            var allRowsEntities = await tableHelpers.GetAllEntitiesAsync(partitionKey);

            // Use a HashSet for fast lookup of expected node names
            var expectedNodes = new HashSet<string>(builderNodeNames, StringComparer.OrdinalIgnoreCase);
            int succeededCount = 0;

            foreach (var rowEntity in allRowsEntities)
            {
                if (rowEntity.TryGetValue("P0", out var value) && value is string json)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("nodeId", out var nodeIdProp) &&
                        doc.RootElement.TryGetProperty("status", out var statusProp))
                    {
                        var nodeName = nodeIdProp.GetString();
                        var nodeStatus = statusProp.GetString();

                        Console.WriteLine($"Node Name: {nodeName}, Node Status: {nodeStatus}");

                        if (!string.IsNullOrEmpty(nodeName) && expectedNodes.Contains(nodeName) &&
                            string.Equals(nodeStatus, "succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            succeededCount++;
                        }
                        else
                        {
                            Assert.Fail($"{nodeName ?? "Unknown Node"} has failed or is not expected.");
                        }
                    }
                    else
                    {
                        Assert.Fail("Missing nodeId or status property in JSON.");
                    }
                }
                else
                {
                    Assert.Fail("Missing or invalid 'P0' property in entity.");
                }
            }

            Assert.That(succeededCount, Is.EqualTo(expectedNodes.Count), "Not all builder nodes succeeded.");
        }

        public static async Task verifyAllBuilderNodesSucceeded(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();
            var allRowsEntities = await tableHelpers.GetAllEntitiesAsync(partitionKey);
                        
            int succeededCount = 0;

            foreach (var rowEntity in allRowsEntities)
            {
                if (rowEntity.TryGetValue("P0", out var value) && value is string json)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("nodeId", out var nodeIdProp) &&
                        doc.RootElement.TryGetProperty("status", out var statusProp))
                    {
                        var nodeName = nodeIdProp.GetString();
                        var nodeStatus = statusProp.GetString();

                        Console.WriteLine($"Node Name: {nodeName}, Node Status: {nodeStatus}");

                        if (!string.IsNullOrEmpty(nodeName) && builderNodeNames.Any(expected => nodeName.Contains(expected)) &&
                            string.Equals(nodeStatus, "succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            succeededCount++;
                        }
                        else
                        {
                            Assert.Fail($"{nodeName ?? "Unknown Node"} has failed or is not expected.");
                        }
                    }
                    else
                    {
                        Assert.Fail("Missing nodeId or status property in JSON.");
                    }
                }
                else
                {
                    Assert.Fail("Missing or invalid 'P0' property in entity.");
                }
            }

            Assert.That(succeededCount, Is.EqualTo(builderNodeNames.Count), "Not all builder nodes succeeded.");
        }
    }
}

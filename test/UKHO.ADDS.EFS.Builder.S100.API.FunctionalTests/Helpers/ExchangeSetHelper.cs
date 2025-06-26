using System.Text.Json;
using Azure.Data.Tables;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

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

        private readonly TableClient _nodeStatusTableClient;
        private readonly TableClient _exchangeSetTimestampTableClient;
        private readonly string _connectionString;
        private readonly string _nodeStatusTable;
        private readonly string _exchangeSetTimestampTable;

        public ExchangeSetHelper()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _connectionString = testConfiguration.AzureStorageConnectionString;
            _nodeStatusTable = testConfiguration.NodeStatusTable;
            _exchangeSetTimestampTable = testConfiguration.ExchangeSetTimestampTable;
            _nodeStatusTableClient = new TableClient(_connectionString, _nodeStatusTable);
            _exchangeSetTimestampTableClient = new TableClient(_connectionString, _exchangeSetTimestampTable);
        }

        public async Task WaitForExchangeSetGeneration(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();

            bool reached = await tableHelpers.WaitForEntityCountAsync(_nodeStatusTableClient, partitionKey, builderNodeNames.Count);
            Assert.That(reached, Is.True, "Exchange Set Generation failed as all the Builder nodes didn't succeed ");
        }

        public async Task verifyAllBuilderNodesSucceeded(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();
            var allRowsEntities = await tableHelpers.GetAllEntitiesAsync(_nodeStatusTableClient, partitionKey);

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

        public async Task verifyExchangeSetTimestampTableEntryUpdated(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();
            var allRowsEntities = await tableHelpers.GetAllEntitiesAsync(_exchangeSetTimestampTableClient, partitionKey);

            bool foundRecentTimestamp = false;
            var nowUtc = DateTime.UtcNow;

            DateTimeOffset timestamp = default;

            var firstEntity = allRowsEntities.FirstOrDefault();
            if (firstEntity != null && firstEntity.TryGetValue("Timestamp", out var value))
            {
                if (value is DateTimeOffset dto)
                {
                    timestamp = dto;
                }
                else if (value is string s && DateTimeOffset.TryParse(s, out var parsed))
                {
                    timestamp = parsed;
                }
                else
                {
                    Assert.Fail("Timestamp property is not a valid DateTimeOffset or ISO 8601 string.");
                }
                
                var diff = (nowUtc - timestamp.UtcDateTime).Duration();
                if (diff <= TimeSpan.FromMinutes(1))
                {
                    foundRecentTimestamp = true;
                }
            }
            else
            {
                Assert.Fail("Missing 'Timestamp' property in entity.");
            }

            Assert.That(foundRecentTimestamp, Is.True, "Timestamp value has not been updated for the current Exchange Set");
        }
    }
}

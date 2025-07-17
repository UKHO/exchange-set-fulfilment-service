using System.Text.Json;
using Azure.Data.Tables;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Facades;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers
{
    public class ExchangeSetHelper
    {
        private static readonly List<string> builderNodeNames = new()
              {
                  "ReadConfigurationNode",
                  "GetBuildNode",
                  "StartTomcatNode",
                  "CheckEndpointsNode",
                  "ProductSearchNode",
                  "DownloadFilesNode",
                  "AddExchangeSetNode",
                  "AddContentExchangeSetNode",
                  "SignExchangeSetNode",
                  "ExtractExchangeSetNode",
                  "UploadFilesNode"
              };

        private readonly TableClient _buildMementoTableClient;
        private readonly TableClient _exchangeSetTimestampTableClient;
        private readonly string _connectionString;
        private readonly string _buildMementoTable;
        private readonly string _exchangeSetTimestampTable;

        public ExchangeSetHelper()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _connectionString = testConfiguration.AzureStorageConnectionString;
            _buildMementoTable = testConfiguration.BuildMementoTable;
            _exchangeSetTimestampTable = testConfiguration.ExchangeSetTimestampTable;
            _buildMementoTableClient = new TableClient(_connectionString, _buildMementoTable);
            _exchangeSetTimestampTableClient = new TableClient(_connectionString, _exchangeSetTimestampTable);
        }

        public async Task WaitForExchangeSetGeneration(string jobId, int maxAttempts = 60, int delayMilliseconds = 1000)
        {
            var orchestratorAPIFacade = new OrchestratorAPIFacade();
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var response = await orchestratorAPIFacade.CheckJobStatus(jobId);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);

                    var root = doc.RootElement;

                    if (root.TryGetProperty("jobState", out var jobStateProp) &&
                        root.TryGetProperty("buildState", out var buildStateProp))
                    {
                        var jobState = jobStateProp.GetString();
                        var buildState = buildStateProp.GetString();

                        if (string.Equals(jobState, "completed", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(buildState, "succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }

                await Task.Delay(delayMilliseconds);
            }

            throw new TimeoutException($"Exchange set generation did not succeed for jobId '{jobId}' within the allotted time.");
        }

        public async Task VerifyAllBuilderNodesSucceeded(string partitionKey)
        {
            var tableHelpers = new AzureTableHelpers();
            var allRowsEntities = await tableHelpers.GetAllEntitiesAsync(_buildMementoTableClient, partitionKey);

            var firstEntity = allRowsEntities.FirstOrDefault();
            if (firstEntity != null && firstEntity.TryGetValue("P0", out var value) && value is string json)
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check builderExitCode  
                if (root.TryGetProperty("builderExitCode", out var exitCodeProp) &&
                    string.Equals(exitCodeProp.GetString(), "success", StringComparison.OrdinalIgnoreCase))
                {
                    // Check all builderSteps statuses and nodeIds  
                    if (root.TryGetProperty("builderSteps", out var stepsProp) && stepsProp.ValueKind == JsonValueKind.Array)
                    {
                        var expectedNodes = new HashSet<string>(builderNodeNames, StringComparer.OrdinalIgnoreCase);
                        var succeededNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var step in stepsProp.EnumerateArray())
                        {
                            string nodeId = "";
                            string status = "";

                            if (step.TryGetProperty("nodeId", out var nodeIdProp))
                                nodeId = nodeIdProp.GetString() ?? string.Empty;

                            if (step.TryGetProperty("status", out var statusProp))
                                status = statusProp.GetString() ?? string.Empty;

                            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(status))
                            {
                                Assert.Fail("A builderStep is missing 'nodeId' or 'status' property.");
                            }

                            if (!expectedNodes.Contains(nodeId))
                            {
                                Assert.Fail($"Unexpected or missing nodeId: '{nodeId}'.");
                            }

                            if (!string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                            {
                                Assert.Fail($"Node '{nodeId}' did not succeed. Status: '{status}'.");
                            }

                            succeededNodes.Add(nodeId);
                        }

                        Assert.That(succeededNodes.SetEquals(expectedNodes), "Not all expected builder nodes succeeded.");
                        return;
                    }
                }
            }
            else
            {
                Assert.Fail("Missing or invalid 'P0' property in entity.");
            }

            Assert.Fail("Not all builder nodes succeeded or response format is invalid.");
        }


        public async Task VerifyExchangeSetTimestampTableEntryUpdated(string partitionKey)
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

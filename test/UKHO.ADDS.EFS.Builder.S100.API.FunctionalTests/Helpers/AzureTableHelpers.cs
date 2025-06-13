using Azure.Data.Tables;
using UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Support;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers  {
    public class AzureTableHelpers
    {
        private readonly TableClient _tableClient;
        private readonly string _connectionString;
        private readonly string _nodeStatusTable;

        public AzureTableHelpers()
        {
            TestConfiguration testConfiguration = new TestConfiguration();
            _connectionString = testConfiguration.AzureStorageConnectionString;
            _nodeStatusTable = testConfiguration.NodeStatusTable;
            _tableClient = new TableClient(_connectionString, _nodeStatusTable);
        }

        public async Task<List<TableEntity>> GetAllEntitiesAsync()
        {
            var entities = new List<TableEntity>();
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                entities.Add(entity);
            }
            return entities;
        }

        public async Task<List<TableEntity>> GetAllEntitiesAsync(string partitionKey)
        {
            var entities = new List<TableEntity>();
            string filter = $"PartitionKey eq '{partitionKey}'"; // Corrected filter syntax
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(filter))
            {
                entities.Add(entity);
            }
            return entities;
        }

        public async Task<bool> WaitForEntityCountAsync(string partitionKey, int expectedCount, int timeoutSeconds = 60, int pollIntervalMs = 1000)
        {
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                var entities = await GetAllEntitiesAsync(partitionKey);
                if (entities.Count >= expectedCount)
                    return true;

                await Task.Delay(pollIntervalMs);
            }

            return false;
        }
    }

}

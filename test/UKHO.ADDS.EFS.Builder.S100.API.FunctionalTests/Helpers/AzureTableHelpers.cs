using Azure.Data.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.API.FunctionalTests.Helpers  {
    public class AzureTableHelpers
    {

        public async Task<List<TableEntity>> GetAllEntitiesAsync(TableClient _tableClient)
        {
            var entities = new List<TableEntity>();
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                entities.Add(entity);
            }
            return entities;
        }

        public async Task<List<TableEntity>> GetAllEntitiesAsync(TableClient _tableClient, string partitionKey)
        {
            var entities = new List<TableEntity>();
            string filter = $"PartitionKey eq '{partitionKey}'";
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(filter))
            {
                entities.Add(entity);
            }
            return entities;
        }

        public async Task<bool> WaitForEntityCountAsync(TableClient _tableClient, string partitionKey, int expectedCount, int timeoutSeconds = 60, int pollIntervalMs = 1000)
        {
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                var entities = await GetAllEntitiesAsync(_tableClient, partitionKey);
                if (entities.Count >= expectedCount)
                    return true;

                await Task.Delay(pollIntervalMs);
            }

            return false;
        }
    }

}

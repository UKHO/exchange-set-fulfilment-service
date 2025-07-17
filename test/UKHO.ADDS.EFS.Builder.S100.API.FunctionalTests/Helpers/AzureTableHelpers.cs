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
    }

}

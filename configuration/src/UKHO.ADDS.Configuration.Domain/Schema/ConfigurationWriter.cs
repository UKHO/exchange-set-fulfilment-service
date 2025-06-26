using Azure.Data.Tables;

namespace UKHO.ADDS.Configuration.Schema
{
    internal class ConfigurationWriter
    {
        private readonly TableServiceClient _tableServiceClient;

        public ConfigurationWriter(TableServiceClient tableServiceClient) => _tableServiceClient = tableServiceClient;

        public async Task WriteConfigurationAsync(AddsEnvironment env, string json)
        {
            var allEnvironments = ConfigurationParser.Parse(json);

            var targetEnv = allEnvironments.FirstOrDefault(e => e.Environment == env);

            if (targetEnv == null)
            {
                throw new InvalidOperationException($"Environment '{env}' not found in the configuration.");
            }

            var tableClient = _tableServiceClient.GetTableClient(WellKnownConfigurationName.ConfigurationServiceTableName);
            await tableClient.CreateIfNotExistsAsync();

            var existingEntities = new Dictionary<(string partitionKey, string rowKey), TableEntity>();
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                existingEntities[(entity.PartitionKey, entity.RowKey)] = entity;
            }

            var desiredEntities = new Dictionary<(string partitionKey, string rowKey), TableEntity>();

            foreach (var service in targetEnv.Services)
            {
                var partitionKey = service.ServiceName;

                foreach (var (propertyPath, prop) in service.Properties)
                {
                    var rowKey = propertyPath;

                    var entity = new TableEntity(partitionKey, rowKey) { ["Value"] = prop.JsonValue?.ToString() ?? string.Empty, ["Type"] = prop.Type ?? string.Empty, ["Required"] = prop.Required, ["Secret"] = prop.Secret };

                    desiredEntities[(partitionKey, rowKey)] = entity;
                }
            }

            // Upsert current entities
            foreach (var kvp in desiredEntities)
            {
                await tableClient.UpsertEntityAsync(kvp.Value, TableUpdateMode.Replace);
            }

            // Remove orphaned entries
            foreach (var existing in existingEntities.Keys.Except(desiredEntities.Keys))
            {
                await tableClient.DeleteEntityAsync(existing.partitionKey, existing.rowKey);
            }
        }
    }
}

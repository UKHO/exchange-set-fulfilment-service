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

        public async Task<bool> WriteConfigurationAsync(IEnumerable<StoredServiceConfiguration> configuration)
        {
            var tableClient = _tableServiceClient.GetTableClient(WellKnownConfigurationName.ConfigurationServiceTableName);

            var hasChanges = false;

            foreach (var service in configuration)
            {
                var existing = await ReadExistingAsync(tableClient, service.ServiceName);
                var existingKeys = new HashSet<string>(existing.Keys, StringComparer.OrdinalIgnoreCase);
                var currentKeys = new HashSet<string>(service.Properties.Keys, StringComparer.OrdinalIgnoreCase);

                foreach (var (key, prop) in service.Properties)
                {
                    var entity = CreateEntity(service.ServiceName, key, prop);

                    if (!existing.TryGetValue(key, out var existingEntity) || !EntityEquals(existingEntity, entity))
                    {
                        await tableClient.UpsertEntityAsync(entity);
                        hasChanges = true;
                    }
                }

                foreach (var key in existingKeys.Except(currentKeys))
                {
                    await tableClient.DeleteEntityAsync(service.ServiceName, key);
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private static TableEntity CreateEntity(string serviceName, string key, StoredProperty prop)
        {
            var entity = new TableEntity(serviceName, key) { ["Value"] = prop.Value ?? string.Empty, ["Type"] = prop.Type ?? string.Empty, ["Required"] = prop.Required, ["Secret"] = prop.Secret };

            return entity;
        }

        private static bool EntityEquals(TableEntity a, TableEntity b) =>
            string.Equals(a.GetString("Value"), b.GetString("Value"), StringComparison.Ordinal) &&
            string.Equals(a.GetString("Type"), b.GetString("Type"), StringComparison.Ordinal) &&
            a.GetBoolean("Required") == b.GetBoolean("Required") &&
            a.GetBoolean("Secret") == b.GetBoolean("Secret");

        private static async Task<Dictionary<string, TableEntity>> ReadExistingAsync(TableClient tableClient, string partitionKey)
        {
            var results = new Dictionary<string, TableEntity>(StringComparer.OrdinalIgnoreCase);
            await foreach (var entity in tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey))
            {
                results[entity.RowKey] = entity;
            }

            return results;
        }
    }
}

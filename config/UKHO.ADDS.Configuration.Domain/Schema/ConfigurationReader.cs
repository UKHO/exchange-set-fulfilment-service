using Azure;
using Azure.Data.Tables;
using Azure.Security.KeyVault.Secrets;

namespace UKHO.ADDS.Configuration.Schema
{
    internal class ConfigurationReader
    {
        private readonly SecretClient _secretClient;
        private readonly TableServiceClient _tableServiceClient;

        public ConfigurationReader(TableServiceClient tableServiceClient, SecretClient secretClient)
        {
            _tableServiceClient = tableServiceClient;
            _secretClient = secretClient;
        }

        public async Task<IEnumerable<StoredServiceConfiguration>> ReadConfigurationAsync(AddsEnvironment environment)
        {
            var tableClient = _tableServiceClient.GetTableClient(WellKnownConfigurationName.ConfigurationServiceTableName);
            var entities = tableClient.QueryAsync<TableEntity>();

            var serviceMap = new Dictionary<string, Dictionary<string, StoredProperty>>(StringComparer.OrdinalIgnoreCase);

            await foreach (var entity in entities)
            {
                var serviceName = entity.PartitionKey;
                var propertyPath = entity.RowKey;

                var storedProperty = new StoredProperty
                {
                    Path = propertyPath,
                    Value = entity.GetString("Value"),
                    Type = entity.GetString("Type"),
                    Required = entity.GetBoolean("Required") ?? false,
                    Secret = entity.GetBoolean("Secret") ?? false
                };

                // If not local and this is a secret, fetch secret value using property value as the secret name
                if (!environment.IsLocal() && storedProperty.Secret && !string.IsNullOrWhiteSpace(storedProperty.Value))
                {
                    try
                    {
                        var secret = await _secretClient.GetSecretAsync(storedProperty.Value);
                        storedProperty.Value = secret.Value.Value;
                    }
                    catch (RequestFailedException ex)
                    {
                        throw new InvalidOperationException($"Failed to retrieve secret '{storedProperty.Value}' from Key Vault.", ex);
                    }
                }

                if (!serviceMap.TryGetValue(serviceName, out var props))
                {
                    props = new Dictionary<string, StoredProperty>(StringComparer.OrdinalIgnoreCase);
                    serviceMap[serviceName] = props;
                }

                props[propertyPath] = storedProperty;
            }

            return serviceMap.Select(kvp => new StoredServiceConfiguration { ServiceName = kvp.Key, Properties = kvp.Value });
        }
    }
}

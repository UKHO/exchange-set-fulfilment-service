using System.Text.Json;
using Azure.Data.Tables;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Seeder
{
    public class ExternalServiceDiscoWriter
    {
        private readonly TableServiceClient _tableServiceClient;

        public ExternalServiceDiscoWriter(TableServiceClient tableServiceClient) =>
            _tableServiceClient = tableServiceClient;

        public async Task WriteExternalServiceDiscoAsync(IEnumerable<DiscoEndpointTemplate> templates)
        {
            var tableClient = _tableServiceClient.GetTableClient(WellKnownConfigurationName.ExternalServiceDiscoTableName);
            await tableClient.CreateIfNotExistsAsync();

            foreach (var template in templates)
            {
                var serialized = JsonSerializer.Serialize(template);

                var entity = new TableEntity(template.Key, template.Key)
                {
                    {
                        "Endpoint", serialized
                    }
                };

                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            }
        }
    }
}

using System.Text.Json;
using Azure.Data.Tables;

namespace UKHO.ADDS.Configuration.Seeder
{
    public class ExternalServiceDiscoWriter
    {
        private const string TableName = "externalsvcdisco";
        private readonly TableServiceClient _tableServiceClient;

        public ExternalServiceDiscoWriter(TableServiceClient tableServiceClient) =>
            _tableServiceClient = tableServiceClient;

        public async Task WriteExternalServiceDiscoAsync(IEnumerable<DiscoEndpointTemplate> templates)
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);
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

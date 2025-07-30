using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace UKHO.ADDS.Configuration.ExternalServices
{
    internal class ExternalServiceRegistry : IExternalServiceRegistry
    {
        private const string TableName = "externalsvcdisco";
        private readonly TableServiceClient _tableServiceClient;

        public ExternalServiceRegistry(TableServiceClient tableServiceClient) => _tableServiceClient = tableServiceClient;

        public async Task<Uri> GetExternalService(string serviceName)
        {
            var tableClient = _tableServiceClient.GetTableClient(TableName);

            try
            {
                var response = await tableClient.GetEntityAsync<TableEntity>(serviceName, serviceName);
                var entity = response.Value;

                if (!entity.TryGetValue("Endpoint", out var json) || json is not string jsonString)
                {
                    throw new InvalidOperationException($"Missing 'Endpoint' column for service '{serviceName}'.");
                }

                var template = JsonSerializer.Deserialize<DiscoEndpointTemplate>(jsonString);

                if (template?.ResolvedUrl is null)
                {
                    throw new InvalidOperationException($"Invalid or missing ResolvedUrl for service '{serviceName}'.");
                }

                return new Uri(template.ResolvedUrl);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new InvalidOperationException($"Service '{serviceName}' was not found in the external service registry.");
            }
        }
    }
}

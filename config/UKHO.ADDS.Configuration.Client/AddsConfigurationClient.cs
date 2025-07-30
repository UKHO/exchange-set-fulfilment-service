using Grpc.Net.Client;
using UKHO.ADDS.Configuration.Grpc;

namespace UKHO.ADDS.Configuration.Client
{
    internal class AddsConfigurationClient
    {
        private readonly ConfigurationService.ConfigurationServiceClient _client;

        public AddsConfigurationClient(HttpClient httpClient)
        {
            var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
            {
                HttpClient = httpClient
            });

            _client = new ConfigurationService.ConfigurationServiceClient(channel);
        }

        public async Task<Dictionary<string, string>> GetConfigurationAsync(string[] serviceNames)
        {
            var request = new ServiceConfigurationRequest();
            request.ServiceNames.AddRange(serviceNames);

            var response = await _client.GetConfigurationAsync(request);

            return response.Properties.ToDictionary(p => p.Path, p => p.Value);
        }
    }
}

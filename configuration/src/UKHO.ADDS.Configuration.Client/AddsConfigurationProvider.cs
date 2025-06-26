using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.Configuration.Client
{
    internal class AddsConfigurationProvider : ConfigurationProvider
    {
        private readonly string _baseUri;
        private readonly string _serviceName;

        public AddsConfigurationProvider(string baseUri, string serviceName)
        {
            _baseUri = baseUri;
            _serviceName = serviceName;
        }

        public override void Load()
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{_baseUri}/grpc");

            var client = new AddsConfigurationClient(httpClient);
            var configuration = client.GetConfigurationAsync(_serviceName).GetAwaiter().GetResult();

            Data = configuration;
        }
    }
}

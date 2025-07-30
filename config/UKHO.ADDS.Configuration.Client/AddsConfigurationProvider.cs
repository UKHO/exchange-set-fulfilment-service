using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Configuration.Client
{
    internal class AddsConfigurationProvider : ConfigurationProvider
    {
        private readonly string _baseUri;
        private readonly string[] _serviceNames;

        public AddsConfigurationProvider(string baseUri, string[] serviceNames)
        {
            _baseUri = baseUri;
            _serviceNames = serviceNames;
        }

        public override void Load()
        {
            // TODO Reinstate when reverting to gRPC
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{_baseUri}");

            // TODO Reinstate when reverting to gRPC
            //httpClient.BaseAddress = new Uri($"{_baseUri}/grpc");

            //var client = new AddsConfigurationClient(httpClient);
            //var configuration = client.GetConfigurationAsync(_serviceNames).GetAwaiter().GetResult();

            //Data = configuration;

            using var response = httpClient.GetAsync("/configuration").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var configurations = JsonCodec.Decode<List<StoredServiceConfiguration>>(json)!;

            foreach (var configuration in configurations)
            {
                if (_serviceNames.Contains(configuration.ServiceName))
                {
                    var dictionary = configuration.Properties;

                    foreach (var prop in dictionary)
                    {
                        Data.Add(prop.Key, prop.Value.Value);
                    }
                }
            }
        }
    }
}

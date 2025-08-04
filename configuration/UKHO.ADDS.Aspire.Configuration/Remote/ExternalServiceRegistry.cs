using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Aspire.Configuration.Remote
{
    internal class ExternalServiceRegistry : IExternalServiceRegistry
    {
        private readonly IConfiguration _configuration;

        public ExternalServiceRegistry(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IExternalEndpoint> GetServiceEndpointAsync(string serviceName, string tag = "", EndpointHostSubstitution host = EndpointHostSubstitution.None)
        {
            var serviceKey = $"{WellKnownConfigurationName.ExternalServiceKeyPrefix}:{serviceName}";
            var serviceDefinitionJson = _configuration[serviceKey]!;

            if (string.IsNullOrEmpty(serviceDefinitionJson))
            {
                throw new KeyNotFoundException($"Service definition for '{serviceName}' not found in configuration.");
            }

            var serviceDefinition = JsonCodec.Decode<ExternalServiceDefinition>(serviceDefinitionJson)!;

            var endpoint = serviceDefinition.Endpoints.FirstOrDefault(e => e.Tag == tag);

            if (endpoint == null)
            {
                throw new KeyNotFoundException($"No endpoint found for service '{serviceName}' with tag '{tag}'.");
            }

            var url = endpoint.ResolvedUrl;

            switch (host)
            {
                case EndpointHostSubstitution.Docker:
                    var builder = new UriBuilder(url)
                    {
                        Host = "host.docker.internal"
                    };

                    url = builder.ToString();
                    break;
                case EndpointHostSubstitution.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(host), host, null);
            }

            return new ExternalEndpoint()
            {
                Host = host,
                Tag = tag,
                Uri = new Uri(url)
            };

        }
    }
}

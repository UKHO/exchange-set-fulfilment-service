using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Configuration.Client
{
    public class ExternalServiceRegistry : IExternalServiceRegistry
    {
        private readonly string _baseUri;
        private List<DiscoEndpointTemplate>? _services;

        public ExternalServiceRegistry(string baseUri)
        {
            _baseUri = baseUri;
        }

        public async Task<Uri?> GetExternalServiceEndpointAsync(string serviceName, bool useDockerHost = false)
        {
            if (_services == null)
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri($"{_baseUri}");

                using var response = await httpClient.GetAsync("/services");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                _services = JsonCodec.Decode<List<DiscoEndpointTemplate>>(json)!;
            }

            var service = _services.FirstOrDefault(s => s.Key.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

            if (service != null)
            {
                if (useDockerHost)
                {
                    var builder = new UriBuilder(service.ResolvedUrl)
                    {
                        Host = "host.docker.internal"
                    };

                    return builder.Uri;
                }

                return new Uri(service.ResolvedUrl);
            }

            return null;
        }
    }
}

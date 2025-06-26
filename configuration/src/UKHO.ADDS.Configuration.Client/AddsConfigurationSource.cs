using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.Configuration.Client
{
    internal class AddsConfigurationSource : IConfigurationSource
    {
        private readonly string _baseUri;
        private readonly string _serviceName;

        public AddsConfigurationSource(string baseUri, string serviceName)
        {
            _baseUri = baseUri;
            _serviceName = serviceName;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new AddsConfigurationProvider(_baseUri, _serviceName);
    }
}

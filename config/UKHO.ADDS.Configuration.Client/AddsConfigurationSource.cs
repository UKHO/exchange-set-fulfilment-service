using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.Configuration.Client
{
    internal class AddsConfigurationSource : IConfigurationSource
    {
        private readonly string _baseUri;
        private readonly string[] _serviceNames;

        public AddsConfigurationSource(string baseUri, string[] serviceNames)
        {
            _baseUri = baseUri;
            _serviceNames = serviceNames;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new AddsConfigurationProvider(_baseUri, _serviceNames);
    }
}

namespace UKHO.Clients.FileShare.Internal
{
    internal class SetBaseAddressHttpClientFactory : IHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactoryImplementation;
        private readonly Uri _baseAddress;

        public SetBaseAddressHttpClientFactory(IHttpClientFactory httpClientFactoryImplementation, Uri baseAddress)
        {
            _httpClientFactoryImplementation = httpClientFactoryImplementation;
            _baseAddress = baseAddress;
        }

        public HttpClient CreateClient(string name)
        {
            var httpClient = _httpClientFactoryImplementation.CreateClient(name);
            httpClient.BaseAddress = _baseAddress;
            return httpClient;
        }
    }
}

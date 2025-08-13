namespace UKHO.ADDS.Clients.Common.Factories
{
    internal class SetBaseAddressHttpClientFactory : IHttpClientFactory
    {
        private readonly Uri _baseAddress;
        private readonly IHttpClientFactory _httpClientFactoryImpl;

        public SetBaseAddressHttpClientFactory(IHttpClientFactory httpClientFactoryImpl, Uri baseAddress)
        {
            _httpClientFactoryImpl = httpClientFactoryImpl;
            _baseAddress = baseAddress;
        }

        public HttpClient CreateClient(string name)
        {
            var httpClient = _httpClientFactoryImpl.CreateClient(name);
            httpClient.BaseAddress = _baseAddress;

            return httpClient;
        }
    }
}

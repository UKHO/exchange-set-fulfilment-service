using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public class SalesCatalogueClientFactory : ISalesCatalogueClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public SalesCatalogueClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public ISalesCatalogueClient CreateClient(string baseAddress, string accessToken) => new SalesCatalogueClient(_clientFactory, baseAddress, accessToken);

        public ISalesCatalogueClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider) => new SalesCatalogueClient(_clientFactory, baseAddress, tokenProvider);
    }
}

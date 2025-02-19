using UKHO.Clients.Common.Configuration;

namespace UKHO.Clients.SalesCatalog
{
    internal class SalesCatalogClientFactory : ISalesCatalogClientFactory
    {
        private readonly ClientConfiguration _configuration;

        public SalesCatalogClientFactory(ClientConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<ISalesCatalogClient> CreateClientAsync() => Task.FromResult<ISalesCatalogClient>(new DummySalesCatalogClient(_configuration));
    }
}

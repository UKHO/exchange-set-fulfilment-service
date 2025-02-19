namespace UKHO.Clients.SalesCatalog
{
    public interface ISalesCatalogClientFactory
    {
        Task<ISalesCatalogClient> CreateClientAsync();
    }
}

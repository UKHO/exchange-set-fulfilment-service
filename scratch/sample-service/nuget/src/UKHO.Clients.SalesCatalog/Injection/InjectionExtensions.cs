using Microsoft.Extensions.DependencyInjection;
using UKHO.Clients.Common.Configuration;

namespace UKHO.Clients.SalesCatalog.Injection
{
    public static class InjectionExtensions
    {

        public static IServiceCollection AddSalesCatalog(this IServiceCollection collection, ClientConfiguration configuration)
        {
            collection.AddSingleton<ISalesCatalogClientFactory>(x => new SalesCatalogClientFactory(configuration));

            return collection;
        }
    }
}

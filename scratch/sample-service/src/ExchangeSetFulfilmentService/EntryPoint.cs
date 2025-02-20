using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Clients.Common.Configuration;
using UKHO.Clients.FileShare.Injection;
using UKHO.Clients.SalesCatalog.Injection;
using UKHO.ExchangeSets.Fulfilment;
using UKHO.ExchangeSets.Fulfilment.Injection;

namespace ExchangeSetFulfilmentService
{
    internal class EntryPoint
    {
        private static async Task<int> Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();

            ConfigureLogging(serviceCollection);
            var serviceProvider = ConfigureInjection(serviceCollection);

            var exchangeSetBuilder = serviceProvider.GetRequiredService<IIExchangeSetBuilder>();
            var result = await exchangeSetBuilder.BuildExchangeSet();

            return (int)result;
        }

        private static void ConfigureLogging(IServiceCollection collection) =>
            collection.AddLogging(builder => { builder.AddConsole(); });

        private static IServiceProvider ConfigureInjection(IServiceCollection collection)
        {
            collection.AddFileShare(new ClientConfiguration { BaseAddress = "https://fileshare" });
            collection.AddSalesCatalog(new ClientConfiguration { BaseAddress = "https://salescatalog" });

            collection.AddFulfilment(new ClientConfiguration { BaseAddress = "https://iicthinggy" });

            return collection.BuildServiceProvider();
        }
    }
}

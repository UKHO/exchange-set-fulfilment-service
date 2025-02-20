using Microsoft.Extensions.DependencyInjection;
using UKHO.Clients.Common.Configuration;
using UKHO.ExchangeSets.Fulfilment.IIC;
using UKHO.ExchangeSets.Fulfilment.Nodes.Builder;
using UKHO.ExchangeSets.Fulfilment.Nodes.Distributor;
using UKHO.ExchangeSets.Fulfilment.Nodes.Setup;

namespace UKHO.ExchangeSets.Fulfilment.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFulfilment(this IServiceCollection services, ClientConfiguration configuration)
        {
            services.AddSingleton<IIicClientFactory>(x => new IicClientFactory(configuration));

            services.AddSingleton<IIExchangeSetBuilder, ExchangeSetBuilder>();

            services.AddTransient<DownloadBatchNode>();
            services.AddTransient<GetCatalogNode>();
            services.AddTransient<GetProductsNode>();

            services.AddTransient<AddExchangeSetContentNode>();
            services.AddTransient<CreateExchangeSetContainerNode>();
            services.AddTransient<DownloadExchangeSetNode>();
            services.AddTransient<SignExchangeSetNode>();

            services.AddTransient<FileShareCommitNode>();
            services.AddTransient<FileShareUploadNode>();

            return services;
        }
    }
}

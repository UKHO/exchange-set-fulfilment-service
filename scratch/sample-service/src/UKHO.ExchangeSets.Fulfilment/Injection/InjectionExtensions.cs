using Microsoft.Extensions.DependencyInjection;
using UKHO.Clients.Common.Configuration;
using UKHO.ExchangeSets.Fulfilment.IIC;

namespace UKHO.ExchangeSets.Fulfilment.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFulfilment(this IServiceCollection services, ClientConfiguration configuration)
        {
            services.AddSingleton<IIicClientFactory>(x => new IicClientFactory(configuration));

            return services;
        }
    }
}

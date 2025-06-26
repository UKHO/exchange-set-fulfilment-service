using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.Configuration.Grpc;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Client
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddConfigurationClient(this IServiceCollection services)
        {
            services.AddHttpClient(WellKnownConfigurationName.ConfigurationServiceName, (sp, client) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var baseUri = configuration[$"{WellKnownConfigurationName.ConfigurationServiceName:serviceUri}"]!;

                client.BaseAddress = new Uri(baseUri);
            });

            services.AddSingleton<ConfigurationClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(WellKnownConfigurationName.ConfigurationServiceName);

                return new ConfigurationClient(httpClient);
            });

            return services;
        }
    }
}

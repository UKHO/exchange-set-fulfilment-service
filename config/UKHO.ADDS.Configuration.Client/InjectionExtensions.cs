using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.Client
{
    public static class InjectionExtensions
    {
        public static IConfigurationBuilder AddConfigurationService(this IConfigurationBuilder builder, params string[] serviceNames)
        {
            var configurationKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__https__0";
            var baseUri = Environment.GetEnvironmentVariable(configurationKey)!;

            builder.Add(new AddsConfigurationSource(baseUri, serviceNames));

            return builder;
        }

        public static IServiceCollection AddExternalServiceDiscovery(this IServiceCollection collection)
        {
            var configurationKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__https__0";
            var baseUri = Environment.GetEnvironmentVariable(configurationKey)!;

            collection.AddSingleton<IExternalServiceRegistry>(x => new ExternalServiceRegistry(baseUri));

            return collection;
        }
    }
}

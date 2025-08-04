using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.ADDS.Aspire.Configuration.Remote;

namespace UKHO.ADDS.Aspire.Configuration
{
    public static class ConfigurationExtensions
    {
        public static TBuilder AddConfiguration<TBuilder>(this TBuilder builder, string serviceName) where TBuilder : IHostApplicationBuilder
        {
            var environment = AddsEnvironment.GetEnvironment();

            if (environment == AddsEnvironment.Local)
            {
                builder.Configuration.AddAzureAppConfiguration(o =>
                {
                    var serviceEnvironmentKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0";
                    var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey)!;

                    var connectionString = $"Endpoint={url};Id=aac-credential;Secret=c2VjcmV0;";

                    o.Connect(connectionString)
                        .Select("*", serviceName.ToLowerInvariant());
                });
            }
            else
            {
                builder.Configuration.AddAzureAppConfiguration(o =>
                {
                    var serviceConnectionStringKey = $"CONNECTIONSTRINGS__{WellKnownConfigurationName.ConfigurationServiceName.ToUpperInvariant()}";
                    var connectionString = Environment.GetEnvironmentVariable(serviceConnectionStringKey)!;

                    o.Connect(connectionString)
                        .Select("*", serviceName.ToLowerInvariant());
                });
            }

            builder.Services.AddSingleton<IExternalServiceRegistry, ExternalServiceRegistry>();

            return builder;
        }
    }
}

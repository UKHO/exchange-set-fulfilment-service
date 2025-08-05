using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using UKHO.ADDS.Aspire.Configuration.Remote;

namespace UKHO.ADDS.Aspire.Configuration
{
    public static class ConfigurationExtensions
    {
        public static TBuilder AddConfiguration<TBuilder>(this TBuilder builder, string serviceName, string componentName, int refreshIntervalSeconds = 30) where TBuilder : IHostApplicationBuilder
        {
            var environment = AddsEnvironment.GetEnvironment();

            if (environment == AddsEnvironment.Local)
            {
                builder.Configuration.AddAzureAppConfiguration(o =>
                {
                    const string serviceEnvironmentKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0";

                    var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey)!;

                    var connectionString = $"Endpoint={url};Id=aac-credential;Secret=c2VjcmV0;";

                    o.Connect(connectionString)
                        .Select("*", serviceName.ToLowerInvariant())
                        .ConfigureRefresh(refresh =>
                        {
                            refresh.Register(WellKnownConfigurationName.ReloadSentinelKey, refreshAll: true, label: serviceName.ToLowerInvariant())
                                .SetRefreshInterval(TimeSpan.FromSeconds(refreshIntervalSeconds)); 
                        });
                });
            }
            else
            {
                builder.Services.AddAzureAppConfiguration();

                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    Retry =
                    {
                        MaxRetries = 10,
                        Mode = Azure.Core.RetryMode.Exponential,
                        Delay = TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(30)
                    }
                });

                builder.Configuration.AddAzureAppConfiguration(options =>
                {
                    Log.Information("CONFIG Azure App Config");

                    var key = $"ConnectionStrings__{componentName.ToLowerInvariant()}";
                    var uriString = Environment.GetEnvironmentVariable(key)!;

                    Log.Information($"CONFIG AAC URI : {uriString}");

                    options.Connect(new Uri(uriString), credential)
                        .Select("*", serviceName.ToLowerInvariant())
                        .ConfigureRefresh(refresh =>
                        {
                            refresh.Register(
                                    WellKnownConfigurationName.ReloadSentinelKey,
                                    refreshAll: true,
                                    label: serviceName.ToLowerInvariant())
                                .SetRefreshInterval(TimeSpan.FromSeconds(refreshIntervalSeconds));
                        });

                    Log.Information("Connected");
                });

                //builder.AddAzureAppConfiguration(componentName.ToLowerInvariant(),null, o =>
                //{
                //    o.Select("*", serviceName.ToLowerInvariant())
                //     .ConfigureRefresh(refresh =>
                //    {
                //        refresh.Register(WellKnownConfigurationName.ReloadSentinelKey, refreshAll: true, label: serviceName.ToLowerInvariant())
                //            .SetRefreshInterval(TimeSpan.FromSeconds(refreshIntervalSeconds));
                //    });


                //});
            }

            builder.Services.AddSingleton<IExternalServiceRegistry, ExternalServiceRegistry>();

            return builder;
        }
    }
}

using Azure.Data.Tables;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.Configuration.Schema;

namespace UKHO.ADDS.Configuration.ExternalServices
{
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddExternalServiceDiscovery(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IExternalServiceRegistry>(x =>
            {
                var config = builder.Configuration;
                var tableStorageConnectionString = config.GetConnectionString(WellKnownConfigurationName.ConfigurationServiceTableStorageName);

                var tableServiceClient = new TableServiceClient(tableStorageConnectionString);

                return new ExternalServiceRegistry(tableServiceClient);
            });

            return builder;
        }
    }
}

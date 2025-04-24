using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator
{
    internal static  class InjectionExtensions
    {
        public static WebApplicationBuilder AddOrchestratorServices(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            builder.Services.AddHttpContextAccessor();

            builder.Services.Configure<JsonOptions>(options => JsonCodec.DefaultOptions.CopyTo(options.SerializerOptions));

            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableClient(StorageConfiguration.TablesName);
            builder.AddAzureBlobClient(StorageConfiguration.BlobsName);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            var queueChannelSize = configuration.GetValue<int>("QueuePolling:ChannelSize");

            builder.Services.AddSingleton(Channel.CreateBounded<ExchangeSetRequestMessage>(new BoundedChannelOptions(queueChannelSize) { FullMode = BoundedChannelFullMode.Wait }));

            builder.Services.AddHostedService<QueuePollingService>();
            builder.Services.AddHostedService<BuilderDispatcherService>();
            builder.Services.AddSingleton<JobService>();

            builder.Services.AddSingleton<ExchangeSetJobTable>();
            builder.Services.AddSingleton<ExchangeSetTimestampTable>();
            builder.Services.AddSingleton<ExchangeSetBuilderNodeStatusTable>();

            // TODO Check once Aspire config stuff is done  
            var salesCatalogueEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.SalesCatalogueEndpoint)!;

            builder.Services.AddSingleton<ISalesCatalogueClientFactory>(provider =>
                new SalesCatalogueClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<ISalesCatalogueClientFactory>();
                return factory.CreateClient(salesCatalogueEndpoint.RemoveControlCharacters(), "");
            });

            return builder;
        }
    }
}

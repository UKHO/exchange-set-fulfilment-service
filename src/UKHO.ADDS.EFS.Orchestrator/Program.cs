using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;
using Serilog;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Middleware;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
#if DEBUG
                .AddJsonFile("appsettings.local.overrides.json")
#endif
                .AddJsonFile("appsettings.Development.json")
                .Build();

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
#if DEBUG
                Log.Logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(configuration)
                                .CreateLogger();

                loggingBuilder.AddSerilog(Log.Logger, dispose: true);
#endif
            });

            ConfigureServices(builder, configuration);

            builder.AddServiceDefaults();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
            }

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseAuthorization();

            RequestsApi.Register(app);
            StatusApi.Register(app);
            JobsApi.Register(app);

            app.Run();
        }

        public static void ConfigureServices(WebApplicationBuilder builder, IConfigurationRoot configuration)
        {
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

            builder.Services.AddSingleton<ExchangeSetJobTable>();
            builder.Services.AddSingleton<ExchangeSetTimestampTable>();
            builder.Services.AddSingleton<ExchangeSetBuilderNodeStatusTable>();

            // TODO Will change once Aspire config stuff is done
            var salesCatalogueEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.SalesCatalogueEndpoint)!;

            builder.Services.AddSingleton<ISalesCatalogueClientFactory>(provider =>
                new SalesCatalogueClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton<ISalesCatalogueClient>(provider =>
            {
                var factory = provider.GetRequiredService<ISalesCatalogueClientFactory>();
                return factory.CreateClient(salesCatalogueEndpoint + "/v2", "");
            });

            builder.Services.AddSingleton(x => new JobService(salesCatalogueEndpoint, x.GetRequiredService<ExchangeSetJobTable>(), x.GetRequiredService<ExchangeSetTimestampTable>(), x.GetRequiredService<ISalesCatalogueClient>(), x.GetRequiredService<ILogger<JobService>>()));
        }
    }
}

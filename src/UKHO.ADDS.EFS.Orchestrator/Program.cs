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
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
#if DEBUG
                Log.Logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(builder.Configuration)
                                .CreateLogger();

                loggingBuilder.AddSerilog(Log.Logger, dispose: true);
#endif
            });

            ConfigureServices(builder);

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

            app.RegisterJobsApi();
            app.RegisterStatusApi();
            app.RegisterRequestsApi();

            app.Run();
        }

        public static void ConfigureServices(WebApplicationBuilder builder)
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
                // Sanitize salesCatalogueEndpoint to prevent log forging  
                var sanitizedEndpoint = salesCatalogueEndpoint.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
                if (string.IsNullOrWhiteSpace(sanitizedEndpoint))
                {
                    throw new ArgumentException("Sales Catalogue Endpoint is invalid or empty.");
                }
                return factory.CreateClient(sanitizedEndpoint, "");
            });

            builder.Services.AddSingleton(x => new JobService(salesCatalogueEndpoint.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim(), x.GetRequiredService<ExchangeSetJobTable>(), x.GetRequiredService<ExchangeSetTimestampTable>(), x.GetRequiredService<ISalesCatalogueClient>(), x.GetRequiredService<ILogger<JobService>>()));
        }
    }
}

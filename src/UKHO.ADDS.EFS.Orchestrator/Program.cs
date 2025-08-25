using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Azure.Identity;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Api;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware;
using UKHO.ADDS.EFS.Orchestrator.Services.Storage;

namespace UKHO.ADDS.EFS.Orchestrator
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
#pragma warning disable LOG001
                Log.Information("Starting the EFS Orchestrator");
#pragma warning restore LOG001

                var builder = WebApplication.CreateBuilder(args);

                var oltpEndpoint = builder.Configuration[GlobalEnvironmentVariables.OtlpEndpoint]!;

                builder.Services.AddHttpContextAccessor();

                //if (builder.Environment.IsDevelopment())
                //{
                //    builder.Services.AddSerilog((services, lc) =>
                //        ConfigureSerilog(lc, services, builder.Configuration, oltpEndpoint)
                //            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                //            .Enrich.WithProperty("System", ServiceConfiguration.ServiceName)
                //            .Enrich.WithProperty("Service", ServiceConfiguration.ServiceName)
                //            .Enrich.WithProperty("NodeName", ServiceConfiguration.NodeName)
                //    );
                //}
                //else
                //{
                    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__efs-events-namespace");
                    var eventHubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME");

                    builder.Services.AddSerilog((services, lc) =>
                        ConfigureSerilog(lc, services, builder.Configuration, oltpEndpoint)
                            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                            .Enrich.WithProperty("System", ServiceConfiguration.ServiceName)
                            .Enrich.WithProperty("Service", ServiceConfiguration.ServiceName)
                            .Enrich.WithProperty("NodeName", ServiceConfiguration.NodeName)
                            .Enrich.FromLogContext()
                            .WriteTo.EventHub(options =>
                            {
                                options.Environment = builder.Environment.EnvironmentName;
                                options.System = ServiceConfiguration.ServiceName;
                                options.Service = ServiceConfiguration.ServiceName;
                                options.NodeName = ServiceConfiguration.NodeName;
                                options.EventHubConnectionString = connectionString;
                                options.EventHubEntityPath = eventHubName;
                                options.TokenCredential = new DefaultAzureCredential();                               
                            })
                    );
                //}

                builder.AddConfiguration(ServiceConfiguration.ServiceName, ProcessNames.ConfigurationService);

                builder.AddServiceDefaults().AddOrchestratorServices();

                builder.AddRedisDistributedCache(ProcessNames.RedisCache);

                var app = builder.Build();

                app.UseSerilogRequestLogging();

                // Configure the HTTP request pipeline.
                //if (app.Environment.IsDevelopment())
                //{
                app.MapOpenApi();
                app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
                //}

                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseAuthorization();

                var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

                app.RegisterJobsApi(loggerFactory);

                var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

                var storageInitializerService = app.Services.GetRequiredService<StorageInitializerService>();
                await storageInitializerService.InitializeStorageAsync(lifetime.ApplicationStopping);

                await app.RunAsync();
            }
            catch (Exception ex)
            {
#pragma warning disable LOG001
                Log.Fatal(ex, "EFS Orchestrator terminated unexpectedly");
#pragma warning restore LOG001
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        // Helper method to configure common Serilog settings
        static LoggerConfiguration ConfigureSerilog(
            LoggerConfiguration loggerConfig,
            IServiceProvider services,
            IConfiguration configuration,
            string oltpEndpoint)
        {
            return loggerConfig
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.OpenTelemetry(o => { o.Endpoint = oltpEndpoint; })
                .WriteTo.Console()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Core", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Queues", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Messaging.EventHubs", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Messaging.EventHubs.EventHubProducerClient", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Messaging.EventHubs.Producer", LogEventLevel.Fatal)
                .MinimumLevel.Override("Microsoft.ApplicationInsights", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Identity", LogEventLevel.Fatal);
        }
    }
}

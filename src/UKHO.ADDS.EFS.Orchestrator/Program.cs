using System.Diagnostics.CodeAnalysis;
using AzureKeyVaultEmulator.Aspire.Client;
using Microsoft.Extensions.Azure;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using UKHO.ADDS.Configuration.Client;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Api;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware;
using UKHO.ADDS.EFS.Orchestrator.Services2.Storage;

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

                builder.Services.AddSerilog((services, lc) => lc
                    .ReadFrom.Configuration(builder.Configuration)
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
                    .MinimumLevel.Override("Azure.Storage.Queues", LogEventLevel.Warning));

                builder.Configuration.AddConfigurationService("UKHO.ADDS.EFS.Orchestrator", "UKHO.ADDS.EFS.Builder.S100");

                builder.AddServiceDefaults()
                    .AddOrchestratorServices();

                var app = builder.Build();

                app.UseSerilogRequestLogging();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference(_ => _.Servers = []); // Stop OpenAPI specifying the wrong port in the generated OpenAPI doc
                }

                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseAuthorization();

                var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

                app.RegisterJobsApi(loggerFactory);
                app.RegisterStatusApi(loggerFactory);
                app.RegisterRequestsApi(loggerFactory);

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
    }
}

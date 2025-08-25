using System.Text.Json;
using Azure.Identity;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Monitors;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Schedule;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Storage;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator
{
    internal static class InjectionExtensions
    {
        private const string OpenApiRequiredType = "string";

        public static WebApplicationBuilder AddOrchestratorServices(this WebApplicationBuilder builder)
        {
            try
            {
                // Use Serilog static logger for structured logging
#pragma warning disable LOG001
                Log.Information("Starting orchestrator service registration");
#pragma warning restore LOG001
                
                var configuration = builder.Configuration;
                var addsEnvironment = AddsEnvironment.GetEnvironment();
                
                // Try to get the ClientId at startup time for logging, but don't rely on it for authentication
                var startupClientId = configuration["orchestrator:ClientId"];
                
                // Debug configuration loading - this helps identify if configuration is loaded yet
#pragma warning disable LOG001
                Log.Information("Configuration debug: Environment={Environment}, StartupClientId={StartupClientId}, HasOrchestratorSection={HasOrchestratorSection}, AllOrchestratorKeys={AllOrchestratorKeys}", 
                    addsEnvironment.ToString(), 
                    startupClientId ?? "NULL", 
                    configuration.GetSection("orchestrator").Exists(),
                    string.Join(", ", configuration.AsEnumerable().Where(c => c.Key.StartsWith("orchestrator", StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Key}={c.Value}")));
#pragma warning restore LOG001

                builder.Services.AddHttpContextAccessor();

                builder.Services.Configure<JsonOptions>(options => JsonCodec.DefaultOptions.CopyTo(options.SerializerOptions));

                // Configure Azure services with logging
#pragma warning disable LOG001
                Log.Information("Configuring Azure services: Queues={QueuesName}, Tables={TablesName}, Blobs={BlobsName}", 
                    StorageConfiguration.QueuesName, StorageConfiguration.TablesName, StorageConfiguration.BlobsName);
#pragma warning restore LOG001
                builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
                builder.AddAzureTableClient(StorageConfiguration.TablesName);
                builder.AddAzureBlobClient(StorageConfiguration.BlobsName);

                builder.Services.AddAuthorization();
                builder.Services.AddOpenApi();

                builder.Services.ConfigureOpenApi();

                builder.Services.AddTransient<IAssemblyPipelineFactory, AssemblyPipelineFactory>();
                builder.Services.AddTransient<AssemblyPipelineNodeFactory>();

                builder.Services.AddTransient<CompletionPipelineFactory>();
                builder.Services.AddTransient<CompletionPipelineNodeFactory>();

                builder.Services.AddSingleton<PipelineContextFactory<S100Build>>();
                builder.Services.AddSingleton<PipelineContextFactory<S63Build>>();
                builder.Services.AddSingleton<PipelineContextFactory<S57Build>>();

                builder.Services.AddHostedService<S100BuildResponseMonitor>();
                builder.Services.AddHostedService<S63BuildResponseMonitor>();
                builder.Services.AddHostedService<S57BuildResponseMonitor>();

                builder.Services.AddSingleton<ITimestampService, TimestampService>();
                builder.Services.AddSingleton<IStorageService, StorageService>();
                builder.Services.AddSingleton<IHashingService, HashingService>();

                builder.Services.AddSingleton<ITable<S100Build>, S100BuildTable>();
                builder.Services.AddSingleton<ITable<S63Build>, S63BuildTable>();
                builder.Services.AddSingleton<ITable<S57Build>, S57BuildTable>();

                builder.Services.AddSingleton<ITable<DataStandardTimestamp>, DataStandardTimestampTable>();
                builder.Services.AddSingleton<ITable<Job>, JobTable>();
                builder.Services.AddSingleton<ITable<BuildMemento>, BuildMementoTable>();

                builder.Services.AddSingleton<IBuilderLogForwarder, BuilderLogForwarder>();
                builder.Services.AddSingleton<StorageInitializerService>();

                // Configure external services with enhanced runtime configuration resolution
                builder.Services.RegisterKiotaClient<KiotaSalesCatalogueService>(provider =>
                {
                    var registry = provider.GetRequiredService<IExternalServiceRegistry>();
                    var scsEndpoint = registry.GetServiceEndpoint(ProcessNames.SalesCatalogueService);

                    // Get the ClientId from runtime configuration when the service is actually resolved
                    var runtimeConfiguration = provider.GetRequiredService<IConfiguration>();
                    var runtimeClientId = runtimeConfiguration["orchestrator:ClientId"];
                    
                    // Enhanced debugging - check all configuration keys at runtime
                    var allOrchestratorKeys = string.Join(", ", runtimeConfiguration.AsEnumerable()
                        .Where(c => c.Key.StartsWith("orchestrator", StringComparison.OrdinalIgnoreCase))
                        .Select(c => $"{c.Key}={c.Value}"));

#pragma warning disable LOG001
                    Log.Information("Configuring external service client: Service={ServiceName}, Environment={Environment}, Endpoint={Endpoint}, StartupClientId={StartupClientId}, RuntimeClientId={RuntimeClientId}, AllRuntimeOrchestratorKeys={AllRuntimeOrchestratorKeys}", 
                        ProcessNames.SalesCatalogueService, addsEnvironment.ToString(), scsEndpoint.Uri?.ToString() ?? "Unknown", 
                        startupClientId ?? "NULL", runtimeClientId ?? "NULL", allOrchestratorKeys);
#pragma warning restore LOG001

                    if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                    {
                        return (scsEndpoint.Uri, new AnonymousAuthenticationProvider());
                    }

                    // Use runtime ClientId for actual authentication, fall back to startup if null
                    var clientIdToUse = runtimeClientId ?? startupClientId;
                    return (scsEndpoint.Uri, new AzureIdentityAuthenticationProvider(new ManagedIdentityCredential(clientId: clientIdToUse), scopes: scsEndpoint.GetDefaultScope()));
                });

                builder.Services.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

                builder.Services.AddSingleton(sp =>
                {
                    var registry = sp.GetRequiredService<IExternalServiceRegistry>();
                    var fssEndpoint = registry.GetServiceEndpoint(ProcessNames.FileShareService);

                    // Get the ClientId from runtime configuration when the service is actually resolved
                    var runtimeConfiguration = sp.GetRequiredService<IConfiguration>();
                    var runtimeClientId = runtimeConfiguration["orchestrator:ClientId"];
                    
                    // Enhanced debugging - check all configuration keys at runtime
                    var allOrchestratorKeys = string.Join(", ", runtimeConfiguration.AsEnumerable()
                        .Where(c => c.Key.StartsWith("orchestrator", StringComparison.OrdinalIgnoreCase))
                        .Select(c => $"{c.Key}={c.Value}"));

#pragma warning disable LOG001
                    Log.Information("Configuring external service client: Service={ServiceName}, Environment={Environment}, Endpoint={Endpoint}, StartupClientId={StartupClientId}, RuntimeClientId={RuntimeClientId}, AllRuntimeOrchestratorKeys={AllRuntimeOrchestratorKeys}", 
                        ProcessNames.FileShareService, addsEnvironment.ToString(), fssEndpoint.Uri?.ToString() ?? "Unknown", 
                        startupClientId ?? "NULL", runtimeClientId ?? "NULL", allOrchestratorKeys);
#pragma warning restore LOG001

                    IAuthenticationTokenProvider? tokenProvider = null;

                    if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                    {
                        tokenProvider = new AnonymousAuthenticationTokenProvider();
                    }
                    else
                    {
                        // Use runtime ClientId for actual authentication, fall back to startup if null
                        var clientIdToUse = runtimeClientId ?? startupClientId;
                        tokenProvider = new TokenCredentialAuthenticationTokenProvider(new ManagedIdentityCredential(clientId: clientIdToUse), [fssEndpoint.GetDefaultScope()]);
                    }

                    var factory = sp.GetRequiredService<IFileShareReadWriteClientFactory>();
                    return factory.CreateClient(fssEndpoint.Uri!.ToString(), tokenProvider);
                });

                builder.Services.AddSingleton<ISalesCatalogueKiotaClientAdapter, SalesCatalogueKiotaClientAdapter>();
                builder.Services.AddSingleton<IOrchestratorSalesCatalogueClient, OrchestratorSalesCatalogueClient>();
                builder.Services.AddSingleton<IOrchestratorFileShareClient, OrchestratorFileShareClient>();

                //Added Dependencies for SchedulerJob
                var exchangeSetGenerationSchedule = configuration["orchestrator:SchedulerJob:ExchangeSetGenerationSchedule"];
#pragma warning disable LOG001
                Log.Information("Configuring Quartz scheduler with cron schedule: {Schedule}", exchangeSetGenerationSchedule ?? "Not configured");
#pragma warning restore LOG001
                
                builder.Services.AddQuartz(q =>
                {
                    var jobKey = new JobKey(nameof(SchedulerJob));
                    q.AddJob<SchedulerJob>(opts => opts.WithIdentity(jobKey));

                    q.AddTrigger(opts => opts
                        .ForJob(jobKey)
                        .WithCronSchedule(exchangeSetGenerationSchedule!, x => x.WithMisfireHandlingInstructionDoNothing())
                    );
                });

                builder.Services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });

#pragma warning disable LOG001
                Log.Information("Orchestrator service registration completed successfully");
#pragma warning restore LOG001
                return builder;
            }
            catch (Exception ex)
            {
#pragma warning disable LOG001
                Log.Fatal(ex, "Service registration failed");
#pragma warning restore LOG001
                throw;
            }
        }

        private static IServiceCollection ConfigureOpenApi(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOpenApi(options =>
            {
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    var headers = context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiHeaderParameter>();

                    foreach (var header in headers)
                    {
                        operation.Parameters ??= new List<OpenApiParameter>();

                        operation.Parameters.Add(new OpenApiParameter
                        {
                            Name = header.Name,
                            In = ParameterLocation.Header,
                            Required = header.Required,
                            Description = header.Description,
                            Schema = new OpenApiSchema { Type = OpenApiRequiredType, Default = new OpenApiString(header.ExpectedValue) }
                        });
                    }

                    return Task.CompletedTask;
                });
            });

            return serviceCollection;
        }
    }
}

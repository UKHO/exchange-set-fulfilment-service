using System.Text.Json;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Quartz;
using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Injection;
using UKHO.ADDS.EFS.Domain.Services.Storage;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Configuration;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;
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
            var configuration = builder.Configuration;
            builder.Services.AddHttpContextAccessor();

            builder.Services.Configure<JsonOptions>(options => JsonCodec.DefaultOptions.CopyTo(options.SerializerOptions));

            builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableClient(StorageConfiguration.TablesName);
            builder.AddAzureBlobClient(StorageConfiguration.BlobsName);

            var addsEnvironment = AddsEnvironment.GetEnvironment();

            // Configure Azure AD settings from configuration
            var azureAdConfig = new EFSAzureADConfiguration();
            configuration.GetSection("orchestrator:EFSAzureADConfiguration").Bind(azureAdConfig);
            builder.Services.Configure<EFSAzureADConfiguration>(configuration.GetSection("orchestrator:EFSAzureADConfiguration"));

            // Validate Azure AD configuration for non-dev/non-local environments
            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                azureAdConfig.ValidateForProductionEnvironment(addsEnvironment.ToString());
            }

            // Configure authentication - compulsory for all environments except dev and local
            builder.Services.AddAuthentication(options =>
            {
                if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                {
                    options.DefaultScheme = "AzureAd";
                }
            });

            if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
            {
                builder.Services.AddAuthentication("AzureAd")
                    .AddJwtBearer("AzureAd", options =>
                    {
                        options.Audience = azureAdConfig.ClientId;
                        options.Authority = azureAdConfig.Authority;
                        options.Events = new JwtBearerEvents
                        {
                            OnForbidden = context =>
                            {
                                context.Response.Headers.Append("origin", "JOBAPI");
                                return Task.CompletedTask;
                            },
                            OnAuthenticationFailed = context =>
                            {
                                context.Response.Headers.Append("origin", "JOBAPI");
                                return Task.CompletedTask;
                            }
                        };
                    });
            }

            builder.Services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("AzureAd")
                    .Build())
                .AddPolicy("ExchangeSetFulfilmentServiceUser", policy => 
                {
                    if (!addsEnvironment.IsLocal() && !addsEnvironment.IsDev())
                    {
                        // For non-dev/non-local environments, authentication is compulsory
                        policy.RequireRole("ExchangeSetFulfilmentServiceUser");
                    }
                    else
                    {
                        // For local and dev environments only, allow anonymous access
                        policy.RequireAssertion(_ => true);
                    }
                });

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

            builder.Services.AddSingleton<IRepository<S100Build>, S100BuildRepository>();
            builder.Services.AddSingleton<IRepository<S63Build>, S63BuildRepository>();
            builder.Services.AddSingleton<IRepository<S57Build>, S57BuildRepository>();

            builder.Services.AddSingleton<IRepository<DataStandardTimestamp>, DataStandardTimestampRepository>();
            builder.Services.AddSingleton<IRepository<Job>, JobRepository>();
            builder.Services.AddSingleton<IRepository<BuildMemento>, BuildMementoRepository>();

            builder.Services.AddSingleton<IBuilderLogForwarder, BuilderLogForwarder>();
            builder.Services.AddSingleton<StorageInitializerService>();

            builder.Services.AddDomain();

            builder.Services.RegisterKiotaClient<KiotaSalesCatalogueService>(provider =>
            {
                var registry = provider.GetRequiredService<IExternalServiceRegistry>();
                var scsEndpoint = registry.GetServiceEndpoint(ProcessNames.SalesCatalogueService);

                if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                {
                    return (scsEndpoint.Uri, new AnonymousAuthenticationProvider());
                }

                return (scsEndpoint.Uri, new AzureIdentityAuthenticationProvider(new ManagedIdentityCredential(), scopes: scsEndpoint.GetDefaultScope()));
            });

            builder.Services.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton(sp =>
            {
                var registry = sp.GetRequiredService<IExternalServiceRegistry>();
                var fssEndpoint = registry.GetServiceEndpoint(ProcessNames.FileShareService);

                IAuthenticationTokenProvider? tokenProvider = null;

                if (addsEnvironment.IsLocal() || addsEnvironment.IsDev())
                {
                    tokenProvider = new AnonymousAuthenticationTokenProvider();
                }
                else
                {
                    tokenProvider = new TokenCredentialAuthenticationTokenProvider(new ManagedIdentityCredential(), [fssEndpoint.GetDefaultScope()]);
                }

                var factory = sp.GetRequiredService<IFileShareReadWriteClientFactory>();
                return factory.CreateClient(fssEndpoint.Uri!.ToString(), tokenProvider);
            });

            builder.Services.AddSingleton<IOrchestratorSalesCatalogueClient, OrchestratorSalesCatalogueClient>();
            builder.Services.AddSingleton<IOrchestratorFileShareClient, OrchestratorFileShareClient>();

            //Added Dependencies for SchedulerJob
            builder.Services.AddQuartz(q =>
            {
                var exchangeSetGenerationSchedule = configuration["orchestrator:SchedulerJob:ExchangeSetGenerationSchedule"];
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

            return builder;
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

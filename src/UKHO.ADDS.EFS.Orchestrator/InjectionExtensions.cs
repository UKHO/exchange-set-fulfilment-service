using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Azure.Identity;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Extensions;
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

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            builder.Services.ConfigureOpenApi();

            builder.Services.AddTransient<AssemblyPipelineFactory>();
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
            builder.Services.AddSingleton<ITable<JobHistory>, JobHistoryTable>();

            builder.Services.AddSingleton<IBuilderLogForwarder, BuilderLogForwarder>();
            builder.Services.AddSingleton<StorageInitializerService>();


            // Register Kiota Defaults and commong authentication provider

            builder.Services.AddKiotaHandlers();
            builder.Services.AddKiotaDefaults(new AnonymousAuthenticationProvider());
            // Uncomment the line below to use Azure Identity for authentication, if required
            //builder.Services.AddKiotaDefaults(new AzureIdentityAuthenticationProvider(new DefaultAzureCredential()));


            // Sales Catalogue Service Kiota
            builder.Services.RegisterKiotaClient<KiotaSalesCatalogueService>("Endpoints:S100SalesCatalogue");

            builder.Services.AddSingleton<ISalesCatalogueClientFactory>(provider => new SalesCatalogueClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<ISalesCatalogueClientFactory>();
                var scsEndpoint = configuration["Endpoints:S100SalesCatalogue"]!;

                return factory.CreateClient(scsEndpoint.RemoveControlCharacters(), string.Empty);
            });

            builder.Services.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
                var fssEndpoint = configuration["Endpoints:S100FileShare"]!;

                return factory.CreateClient(fssEndpoint.RemoveControlCharacters(), string.Empty);
            });

            builder.Services.AddSingleton<IOrchestratorSalesCatalogueClient, OrchestratorSalesCatalogueClient>();
            builder.Services.AddSingleton<IOrchestratorFileShareClient, OrchestratorFileShareClient>();

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

        public static IHttpClientBuilder AddConfiguredHttpClient2<TClient>(this IServiceCollection services, string endpointConfigKey,
                                                                          IDictionary<string, string>? headers = null)
            where TClient : class
        {
            string test = "";
            var result =  services.AddHttpClient<TClient>((provider, client) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var endpoint = config[endpointConfigKey]!;
                client.BaseAddress = new Uri(endpointConfigKey);
                test = endpoint;
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            });
            return result;
        }

        public static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
        {
            var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
            var kiotaHandlers2 = KiotaClientFactory.GetDefaultHandlerTypes();

            foreach (var handler in kiotaHandlers)
            {
                services.AddTransient(handler);
            }
            return services;
        }
    }
}

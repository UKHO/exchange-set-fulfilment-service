using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
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
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Monitors;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;
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

            builder.Services.AddHostedService<JobRequestQueueMonitor>();

            builder.Services.AddHostedService<S100BuildResponseMonitor>();
            builder.Services.AddHostedService<S63BuildResponseMonitor>();
            builder.Services.AddHostedService<S57BuildResponseMonitor>();

            builder.Services.AddSingleton<ITable<S100Build>, S100BuildTable>();
            builder.Services.AddSingleton<ITable<S63Build>, S63BuildTable>();
            builder.Services.AddSingleton<ITable<S57Build>, S57BuildTable>();

            builder.Services.AddTransient<ITable<DataStandardTimestamp>, DataStandardTimestampTable>();
            builder.Services.AddTransient<ITable<Job>, JobTable>();
            builder.Services.AddTransient<ITable<JobHistory>, JobHistoryTable>();

            builder.Services.AddTransient<BuilderLogForwarder>();
            builder.Services.AddTransient<StorageInitializerService>();

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
    }
}

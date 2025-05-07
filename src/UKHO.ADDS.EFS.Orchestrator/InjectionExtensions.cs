using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Extensions;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Tables;
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

            var queueChannelSize = configuration.GetValue<int>("QueuePolling:ChannelSize");

            builder.Services.AddSingleton(Channel.CreateBounded<ExchangeSetRequestQueueMessage>(new BoundedChannelOptions(queueChannelSize) { FullMode = BoundedChannelFullMode.Wait }));

            builder.Services.AddHostedService<QueuePollingService>();
            builder.Services.AddHostedService<BuilderDispatcherService>();
            builder.Services.AddSingleton<JobService>();

            builder.Services.AddSingleton<ExchangeSetJobTable>();
            builder.Services.AddSingleton<ExchangeSetTimestampTable>();
            builder.Services.AddSingleton<ExchangeSetBuilderNodeStatusTable>();

            var salesCatalogueEndpoint = builder.Configuration[OrchestratorEnvironmentVariables.SalesCatalogueEndpoint]!;

            builder.Services.AddSingleton<ISalesCatalogueClientFactory>(provider =>
                new SalesCatalogueClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<ISalesCatalogueClientFactory>();
                return factory.CreateClient(salesCatalogueEndpoint.RemoveControlCharacters(), string.Empty);
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
                            Schema = new OpenApiSchema
                            {
                                Type = OpenApiRequiredType,
                                Default = new OpenApiString(header.ExpectedValue)
                            }
                        });
                    }

                    return Task.CompletedTask;
                });
            });

            return serviceCollection;
        }
    }
}

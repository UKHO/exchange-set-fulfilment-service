﻿using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UKHO.ADDS.Aspire.Configuration.Remote;
using Quartz;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.SalesCatalogueService;
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

namespace UKHO.ADDS.EFS.Orchestrator;

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

        // Configure Kiota services and handlers
        builder.Services.AddKiotaDefaults(new AnonymousAuthenticationProvider());

        // Register the IRequestAdapter manually since we're not using the standard RegisterKiotaClient pattern
        builder.Services.AddSingleton<Microsoft.Kiota.Abstractions.IRequestAdapter>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var authenticationProvider = provider.GetRequiredService<IAuthenticationProvider>();
            var httpClient = httpClientFactory.CreateClient();
            return new HttpClientRequestAdapter(authenticationProvider, httpClient: httpClient);
        });

        // File Share Service Kiota Client
        //builder.Services.RegisterKiotaClient<KiotaFileShareServiceReadWrite>("FileShareService:Endpoint");

        builder.Services.AddHttpClient<KiotaFileShareServiceReadWrite>()
            .AddHttpMessageHandler<HeadersInspectionHandler>();
        builder.Services.AddSingleton<KiotaFileShareServiceReadWrite>(provider =>
        {
            var registry = provider.GetRequiredService<IExternalServiceRegistry>();
            var fssEndpoint = registry.GetServiceEndpointAsync(ProcessNames.FileShareService).GetAwaiter().GetResult();
            var requestAdapter = provider.GetRequiredService<Microsoft.Kiota.Abstractions.IRequestAdapter>();
            requestAdapter.BaseUrl = fssEndpoint!.Uri!.ToString();
            return new KiotaFileShareServiceReadWrite(requestAdapter);
        });

        // Legacy File Share Service client (kept for backwards compatibility during transition)
        builder.Services.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));
        builder.Services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
            var registry = provider.GetRequiredService<IExternalServiceRegistry>();
            var fssEndpoint = registry.GetServiceEndpointAsync(ProcessNames.FileShareService).GetAwaiter().GetResult();
            return factory.CreateClient(fssEndpoint!.Uri!.ToString(), string.Empty);
        });

        builder.Services.AddSingleton<ISalesCatalogueClientFactory>(provider => new SalesCatalogueClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

        builder.Services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<ISalesCatalogueClientFactory>();
            var registry = provider.GetRequiredService<IExternalServiceRegistry>();

            var scsEndpoint = registry.GetServiceEndpointAsync(ProcessNames.SalesCatalogueService).GetAwaiter().GetResult();

            return factory.CreateClient(scsEndpoint.Uri!.ToString(), string.Empty);
        });

        // Legacy File Share Service client (kept for backwards compatibility during transition)
        builder.Services.AddSingleton<IFileShareReadWriteClientFactory>(provider => new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

        builder.Services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
            var registry = provider.GetRequiredService<IExternalServiceRegistry>();

            var fssEndpoint = registry.GetServiceEndpointAsync(ProcessNames.FileShareService).GetAwaiter().GetResult();

            return factory.CreateClient(fssEndpoint.Uri!.ToString(), string.Empty);
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

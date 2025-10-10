using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Quartz;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Services.Injection;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Injection;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers;
using UKHO.ADDS.EFS.Orchestrator.Health;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Generators;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Monitors;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Factories;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Schedule;
using UKHO.ADDS.EFS.Orchestrator.Services;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;
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

            // Add FluentValidation
            builder.Services.AddValidatorsFromAssemblyContaining<S100ProductNamesRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<S100ProductVersionsRequestValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<S100UpdateSinceRequestValidator>();

            builder.AddAzureQueueServiceClient(StorageConfiguration.QueuesName);
            builder.AddAzureTableServiceClient(StorageConfiguration.TablesName);
            builder.AddAzureBlobServiceClient(StorageConfiguration.BlobsName);

            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            builder.Services.ConfigureOpenApi();

            builder.Services.AddTransient<IAssemblyPipelineFactory, AssemblyPipelineFactory>();
            builder.Services.AddTransient<AssemblyPipelineNodeFactory>();
            builder.Services.AddTransient<IAssemblyPipelineNodeFactory, AssemblyPipelineNodeFactory>();

            builder.Services.AddTransient<ICompletionPipelineFactory, CompletionPipelineFactory>();
            builder.Services.AddTransient<ICompletionPipelineNodeFactory, CompletionPipelineNodeFactory>();

            builder.Services.AddTransient<IS100ProductNamesRequestValidator, S100ProductNamesRequestValidator>();
            builder.Services.AddTransient<IS100ProductVersionsRequestValidator, S100ProductVersionsRequestValidator>();
            builder.Services.AddTransient<IS100UpdateSinceRequestValidator, S100UpdateSinceRequestValidator>();

            builder.Services.AddTransient<IExchangeSetResponseFactory, ExchangeSetResponseFactory>();

            builder.Services.AddSingleton<IPipelineContextFactory<S100Build>, PipelineContextFactory<S100Build>>();
            builder.Services.AddSingleton<IPipelineContextFactory<S63Build>, PipelineContextFactory<S63Build>>();
            builder.Services.AddSingleton<IPipelineContextFactory<S57Build>, PipelineContextFactory<S57Build>>();

            builder.Services.AddHostedService<S100BuildResponseMonitor>();
            builder.Services.AddHostedService<S63BuildResponseMonitor>();
            builder.Services.AddHostedService<S57BuildResponseMonitor>();

            builder.Services.AddSingleton<IBuilderLogForwarder, BuilderLogForwarder>();
            builder.Services.AddSingleton<StorageInitializerService>();
            builder.Services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
            builder.Services.AddSingleton<IScsResponseHandler, ScsResponseHandler>();
            builder.Services.AddDomain();
            builder.Services.AddInfrastructure();

            // Register health checks
            builder.Services.AddHealthChecks()
                .AddCheck<FileShareServiceHealthCheck>(
                    "file-share-service",
                    HealthStatus.Unhealthy,
                    tags: new[] { "live", "external-dependency" });

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

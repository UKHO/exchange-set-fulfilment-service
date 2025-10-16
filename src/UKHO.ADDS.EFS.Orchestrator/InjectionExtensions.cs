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
using UKHO.ADDS.EFS.Domain.User;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Injection;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
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

            builder.Services.AddScoped(x =>
            {
                var accessor = x.GetRequiredService<IHttpContextAccessor>();
                var identity = accessor.HttpContext?.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? string.Empty;
                return new UserIdentifier() { Identity = identity };
            });

            return builder;
        }

        private static IServiceCollection ConfigureOpenApi(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOpenApi(options =>
            {
                // Set OpenAPI document info (title, version, description, servers, contact, externalDocs, security)
                _ = options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "S100 Exchange Set Service API",
                        Version = "1.0",
                        Description = "API for generating S100 exchange sets.",
                        Contact = new OpenApiContact
                        {
                            Name = "Abzu Delivery Team",
                            Email = "Abzudeliveryteam@UKHO.gov.uk"
                        }
                    };
                    document.ExternalDocs = new OpenApiExternalDocs
                    {
                        Url = new Uri("https://github.com/UKHO/exchange-set-fulfilment-service")
                    };
                    document.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = "https://exchangesetservice.admiralty.co.uk" }
                    };
                    // Add JWT Bearer security scheme directly to the document
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
                    document.Components.SecuritySchemes["jwtBearerAuth"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "JWT Authorization header using the Bearer scheme."
                    };
                    // Add security requirement
                    document.SecurityRequirements = new List<OpenApiSecurityRequirement>
                    {
                        new OpenApiSecurityRequirement
                        {
                            [ new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "jwtBearerAuth" } } ] = new List<string>()
                        }
                    };
                    return Task.CompletedTask;
                });

                // Add security scheme and response descriptions
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

                    if (operation.Responses.ContainsKey("401"))
                    {
                        operation.Responses["401"].Description = "Unauthorised - either you have not provided any credentials, or your credentials are not recognised.";
                    }
                    if (operation.Responses.ContainsKey("403"))
                    {
                        operation.Responses["403"].Description = "Forbidden - you have been authorised, but you are not allowed to access this resource.";
                    }
                    if (operation.Responses.ContainsKey("429"))
                    {
                        operation.Responses["429"].Description = "You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.";
                    }
                    // Always add 500 Internal Server Error response if missing
                    var error500Example = new OpenApiObject
                    {
                        ["correlationId"] = new OpenApiString("string"),
                        ["detail"] = new OpenApiString("string")
                    };
                    if (!operation.Responses.ContainsKey("500"))
                    {
                        operation.Responses["500"] = new OpenApiResponse
                        {
                            Description = "Internal Server Error.",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.Schema,
                                            Id = "InternalServerError"
                                        }
                                    },
                                    Example = error500Example
                                }
                            }
                        };
                    }
                    else
                    {
                        operation.Responses["500"].Description = "Internal Server Error.";
                        operation.Responses["500"].Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = "InternalServerError"
                                    }
                                },
                                Example = error500Example
                            }
                        };
                    }

                    return Task.CompletedTask;
                });
            });

            return serviceCollection;
        }
    }
}

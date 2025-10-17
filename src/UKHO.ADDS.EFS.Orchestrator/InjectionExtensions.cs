using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Quartz;
using UKHO.ADDS.Clients.Common.Constants;
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

            // Parameter and response descriptions
            const string CallbackUriDescription =
                "An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. " +
                "The data for the notification will follow the CloudEvents 1.0 standard, with the data portion containing the same Exchange Set data as the response to the original API request. " +
                "If not specified, then no call back notification will be sent. Must be a valid HTTPS endpoint.";

            const string ProductIdentifierDescription =
                "An optional identifier parameter determines the product identifier of S-100 Exchange Set. " +
                "If the value is s101, the S-100 Exchange Set will give updates specific to s101 products only. " +
                "The default value of identifier is s100, which means the S-100 Exchange Set will give updated for all product identifier.\r\n\r\n" +
                "Available values : s101, s102, s104, s111";

            const string XCorrelationIdHeaderKeyDesciption = "Unique GUID.";

            const string AcceptedDescription =
                "Request to create Exchange Set is accepted. Response body has Exchange Set status URL to track changes to the status of the task. " +
                "It also contains the URL that the Exchange Set will be available on as well as the number of products in that Exchange Set.";

            const string UnauthorizedDescription =
                "Unauthorised - either you have not provided any credentials, or your credentials are not recognised.";

            const string ForbiddenDescription =
                "Forbidden - you have been authorised, but you are not allowed to access this resource.";

            const string TooManyRequestsDescription =
                "You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.";

            const string NotModifiedDescription =
                "If there are no updates since the sinceDateTime parameter, then a 'Not modified' response will be returned.";

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
                    document.Servers =
                    [
                        new OpenApiServer { Url = "https://exchangesetservice.admiralty.co.uk" }
                    ];
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
                    document.SecurityRequirements =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [ new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "jwtBearerAuth" } } ] = []
                        }
                    ];
                    return Task.CompletedTask;
                });

                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    var headers = context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiHeaderParameter>();
                    operation.Parameters ??= [];
                    foreach (var header in headers)
                    {
                        operation.Parameters.Add(new OpenApiParameter
                        {
                            Name = header.Name,
                            In = ParameterLocation.Header,
                            Required = header.Required,
                            Description = header.Description,
                            Schema = new OpenApiSchema { Type = OpenApiRequiredType, Default = new OpenApiString(header.ExpectedValue) }
                        });
                    }

                    // Set parameter descriptions
                    foreach (var param in operation.Parameters)
                    {
                        param.Description = param.Name switch
                        {
                            "callbackUri" => CallbackUriDescription,
                            "productIdentifier" => ProductIdentifierDescription,
                            ApiHeaderKeys.XCorrelationIdHeaderKey => XCorrelationIdHeaderKeyDesciption,
                            _ => param.Description
                        };
                    }

                    // Set response descriptions
                    var responseDescriptions = new Dictionary<string, string>
                    {
                        ["202"] = AcceptedDescription,
                        ["401"] = UnauthorizedDescription,
                        ["403"] = ForbiddenDescription,
                        ["429"] = TooManyRequestsDescription,
                        ["304"] = NotModifiedDescription
                    };
                    foreach (var (status, desc) in responseDescriptions)
                    {
                        if (operation.Responses.TryGetValue(status, out var response))
                        {
                            response.Description = desc;
                        }
                    }

                    // add 500 Internal Server Error response
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
                    return Task.CompletedTask;
                });
            });
            return serviceCollection;
        }
    }
}

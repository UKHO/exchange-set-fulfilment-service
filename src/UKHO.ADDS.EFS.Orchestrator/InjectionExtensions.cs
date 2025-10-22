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
        private const string CallbackUriDescription =
            "An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. " +
            "The data for the notification will follow the CloudEvents 1.0 standard, with the data portion containing the same Exchange Set data as the response to the original API request. " +
            "If not specified, then no call back notification will be sent. Must be a valid HTTPS endpoint.";
        private const string ProductIdentifierDescription =
            "An optional identifier parameter determines the product identifier of S-100 Exchange Set. " +
            "If the value is s101, the S-100 Exchange Set will give updates specific to s101 products only. " +
            "The default value of identifier is s100, which means the S-100 Exchange Set will give updated for all product identifier.\r\n\r\n" +
            "Available values : s101, s102, s104, s111";
        private const string XCorrelationIdHeaderKeyDesciption = "Unique GUID.";

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

            // SchedulerJob dependencies
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
                return new UserIdentifier { Identity = identity };
            });

            return builder;
        }

        private static IServiceCollection ConfigureOpenApi(this IServiceCollection serviceCollection)
        {
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
                    document.Servers = [ new OpenApiServer { Url = "https://exchangesetservice.admiralty.co.uk" } ];
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
                    // Add InternalServerError schema
                    document.Components.Schemas["InternalServerError"] = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["correlationId"] = new OpenApiSchema { Type = "string" },
                            ["detail"] = new OpenApiSchema { Type = "string" }
                        }
                    };

                    // Add synthetic /auth/client_credentials endpoint (OpenAPI only, not implemented in backend)                    
                    document.Paths ??= new OpenApiPaths();
                    document.Paths["/auth/client_credentials"] = new OpenApiPathItem
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Post] = new OpenApiOperation
                            {
                                Summary = "Get token from AAD",
                                Description = "Returns a token direct from Azure AD using Client Credentials.",
                                Tags = new List<OpenApiTag> { new() { Name = "auth" } },
                                RequestBody = new OpenApiRequestBody
                                {
                                    Required = true,
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = "object",
                                                Properties = new Dictionary<string, OpenApiSchema>
                                                {
                                                    ["client_id"] = new OpenApiSchema { Type = "string" },
                                                    ["client_secret"] = new OpenApiSchema { Type = "string" }
                                                },
                                                Required = new HashSet<string> { "client_id", "client_secret" }
                                            }
                                        }
                                    }
                                },
                                Responses = new OpenApiResponses
                                {
                                    ["200"] = new OpenApiResponse
                                    {
                                        Description = "OK",
                                        Content = new Dictionary<string, OpenApiMediaType>
                                        {
                                            ["application/json"] = new OpenApiMediaType
                                            {
                                                Schema = new OpenApiSchema
                                                {
                                                    Type = "object",
                                                    Properties = new Dictionary<string, OpenApiSchema>
                                                    {
                                                        ["token_type"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("Bearer") },
                                                        ["expires_in"] = new OpenApiSchema { Type = "integer", Format = "int32", Example = new OpenApiInteger(3599) },
                                                        ["ext_expires_in"] = new OpenApiSchema { Type = "integer", Format = "int32", Example = new OpenApiInteger(3599) },
                                                        ["access_token"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("eyJ0eXAiOiJKV1QiLCJhbGciOiJSU") },
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    ["400"] = new OpenApiResponse
                                    {
                                        Description = "Bad request - Request missing client_id and/or client_secret.",
                                        Content = new Dictionary<string, OpenApiMediaType>
                                        {
                                            ["application/json"] = new OpenApiMediaType
                                            {
                                                Schema = new OpenApiSchema
                                                {
                                                    Type = "object",
                                                    Properties = new Dictionary<string, OpenApiSchema>
                                                    {
                                                        ["correlationId"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("184ef711-b039-4c24-b81a-89081d8f324c") },

                                                        ["errors"] = new OpenApiSchema
                                                        {
                                                            Type = "object",
                                                            Properties = new Dictionary<string, OpenApiSchema>
                                                            {
                                                                ["source"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("request") },
                                                                ["description"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("request missing client_id and/or client_secret") },
                                                            }
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    },
                                    ["401"] = new OpenApiResponse { Description = UnauthorizedDescription },
                                    ["403"] = new OpenApiResponse { Description = ForbiddenDescription },
                                    ["429"] = BuildTooManyRequestsResponse(TooManyRequestsDescription)
                                }
                            }
                        }
                    };
                    return Task.CompletedTask;
                });

                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    var headers = context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiHeaderParameter>();
                    AddOpenApiHeaderParameters(operation, headers);
                    SetParameterDescriptions(operation);
                    SetResponseDescriptions(operation, new Dictionary<string, string>
                    {
                        ["202"] = AcceptedDescription,
                        ["401"] = UnauthorizedDescription,
                        ["403"] = ForbiddenDescription,                        
                        ["304"] = NotModifiedDescription
                    });
                                        
                    operation.Responses["429"] = BuildTooManyRequestsResponse(TooManyRequestsDescription);                  
                    AddOpenApiExamples(operation, context.Description.RelativePath, context.Description.HttpMethod);
                    AddInternalServerErrorResponse(operation);

                    // Add Callbacks for custom exchangeset endpoints
                    if (context.Description.HttpMethod == "POST" &&
                        (context.Description.RelativePath?.Equals("v2/exchangeSet/s100/productNames", StringComparison.OrdinalIgnoreCase) == true ||
                         context.Description.RelativePath?.Equals("v2/exchangeSet/s100/productVersions", StringComparison.OrdinalIgnoreCase) == true ||
                         context.Description.RelativePath?.Equals("v2/exchangeSet/s100/updatesSince", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        operation.Callbacks ??= new Dictionary<string, OpenApiCallback>();
                        var callbackExample = BuildCallbackExample();
                        var callbackResponses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "The service will ignore all response from the callback"
                            }
                        };
                        var callback = new OpenApiCallback
                        {
                            PathItems = new Dictionary<Microsoft.OpenApi.Expressions.RuntimeExpression, OpenApiPathItem>
                            {
                                [Microsoft.OpenApi.Expressions.RuntimeExpression.Build("{$request.query.callbackUri}")] = new OpenApiPathItem
                                {
                                    Operations = new Dictionary<OperationType, OpenApiOperation>
                                    {
                                        [OperationType.Post] = new OpenApiOperation
                                        {
                                            Summary = "Notify the Exchange Set requestor that this is now ready to download on the File Share Service.",
                                            Description = "Once the Exchange Set has been committed on File Share Service, a notification will be sent to the callbackURI (if specified).\n\nData:\r\nThe data for the notification will follow the CloudEvents 1.0 standard, with the data portion containing the same S-100 Exchange Set data as the response to the original API request ( $ref: \"#/components/schemas/s100ExchangeSetResponse\" ).",
                                            RequestBody = new OpenApiRequestBody
                                            {
                                                Content = new Dictionary<string, OpenApiMediaType>
                                                {
                                                    ["application/json"] = new OpenApiMediaType
                                                    {
                                                        Schema = new OpenApiSchema { Type = "object" },
                                                        Example = callbackExample
                                                    }
                                                }
                                            },
                                            Responses = callbackResponses
                                        }
                                    }
                                }
                            }
                        };
                        operation.Callbacks["s100FulfilmentResponse"] = callback;
                    }

                    return Task.CompletedTask;
                });
            });
            return serviceCollection;
        }

        private static OpenApiResponse BuildTooManyRequestsResponse(string TooManyRequestsDescription) => new OpenApiResponse
        {
            Description = TooManyRequestsDescription,
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["Retry-After"] = new OpenApiHeader
                {
                    Description = "Specifies the time you should wait in seconds before retrying.",
                    Schema = new OpenApiSchema
                    {
                        Type = "integer"
                    }
                }
            }
        };

        private static OpenApiObject BuildCallbackExample() =>
            new OpenApiObject
            {
                ["specversion"] = new OpenApiString("1.0"),
                ["type"] = new OpenApiString("uk.co.admiralty.s100Data.exchangeSetCreated.v1"),
                ["source"] = new OpenApiString("https://exchangeset.admiralty.co.uk/s100Data"),
                ["id"] = new OpenApiString("2f03a25f-28b3-46ea-b009-5943250a9a41"),
                ["time"] = new OpenApiString("2021-02-17T14:04:04.4880776Z"),
                ["subject"] = new OpenApiString("Requested S-100 Exchange Set Created"),
                ["datacontenttype"] = new OpenApiString("application/json"),
                ["data"] = new OpenApiObject
                {
                    ["_links"] = new OpenApiObject
                    {
                        ["exchangeSetBatchStatusUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status") },
                        ["exchangeSetBatchDetailsUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272") },
                        ["exchangeSetFileUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip") },
                        ["errorFileUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/error.txt") }
                    },
                    ["exchangeSetUrlExpiryDateTime"] = new OpenApiString("2021-02-17T16:19:32.269Z"),
                    ["requestedProductCount"] = new OpenApiInteger(4),
                    ["exchangeSetProductCount"] = new OpenApiInteger(1),
                    ["requestedProductsAlreadyUpToDateCount"] = new OpenApiInteger(0),
                    ["requestedProductsNotInExchangeSet"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["productName"] = new OpenApiString("101GB40079ABCDEFG"),
                            ["reason"] = new OpenApiString("invalidProduct")
                        }
                    },
                    ["fssBatchId"] = new OpenApiString("7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272")
                }
            };

        private static void AddOpenApiHeaderParameters(OpenApiOperation operation, IEnumerable<OpenApiHeaderParameter> headers)
        {
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
        }

        private static void SetParameterDescriptions(OpenApiOperation operation)
        {
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
        }

        private static void SetResponseDescriptions(OpenApiOperation operation, Dictionary<string, string> responseDescriptions)
        {
            foreach (var (status, desc) in responseDescriptions)
            {
                if (operation.Responses.TryGetValue(status, out var response))
                {
                    response.Description = desc;
                }
            }            
        }

        private static void AddOpenApiExamples(OpenApiOperation operation, string relativePath, string httpMethod)
        {
            if (relativePath?.Equals("v2/exchangeSet/s100/productNames", StringComparison.OrdinalIgnoreCase) == true &&
                httpMethod?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true)
            {
                var requestExample = new OpenApiArray
                {
                    new OpenApiString("101HR17QFG4"),
                    new OpenApiString("102CA5QUF3C"),
                    new OpenApiString("104EA4ZL566"),
                    new OpenApiString("111AR401R12")
                };
                if (operation.RequestBody?.Content?.ContainsKey("application/json") == true)
                {
                    operation.RequestBody.Content["application/json"].Example = requestExample;
                    operation.RequestBody.Description = "A list of S-100 product names for which the Exchange Set is requested.";
                }
                var responseExample = new OpenApiObject
                {
                    ["links"] = new OpenApiObject
                    {
                        ["exchangeSetBatchStatusUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://filesvnexte2e.admiralty.co.uk/batch/22c68246-87ae-4f7e-8556-8ee9eeb95037/status") },
                        ["exchangeSetBatchDetailsUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://filesvnexte2e.admiralty.co.uk/batch/22c68246-87ae-4f7e-8556-8ee9eeb95037") },
                        ["exchangeSetFileUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://filesvnexte2e.admiralty.co.uk/batch/22c68246-87ae-4f7e-8556-8ee9eeb95037/files/V01X01.zip") }
                    },
                    ["exchangeSetUrlExpiryDateTime"] = new OpenApiString("2025-10-23T11:22:40.388Z"),
                    ["requestedProductCount"] = new OpenApiInteger(4),
                    ["exchangeSetProductCount"] = new OpenApiInteger(3),
                    ["requestedProductsAlreadyUpToDateCount"] = new OpenApiInteger(0),
                    ["requestedProductsNotInExchangeSet"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["productName"] = new OpenApiString("111AR401R12"),
                            ["reason"] = new OpenApiString("invalidProduct")
                        }
                    },
                    ["fssBatchId"] = new OpenApiString("22c68246-87ae-4f7e-8556-8vc9cvb95037")
                };
                if (operation.Responses.TryGetValue("202", out var acceptedResponse) &&
                    acceptedResponse.Content?.ContainsKey("application/json") == true)
                {
                    acceptedResponse.Content["application/json"].Example = responseExample;
                }
            }
            else if (relativePath?.Equals("v2/exchangeSet/s100/productVersions", StringComparison.OrdinalIgnoreCase) == true &&
                httpMethod?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true)
            {
                var requestExample = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("101GB40079ABCDEFG"),
                        ["editionNumber"] = new OpenApiInteger(5),
                        ["updateNumber"] = new OpenApiInteger(10)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("101DE00904820801012"),
                        ["editionNumber"] = new OpenApiInteger(36),
                        ["updateNumber"] = new OpenApiInteger(5)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("102CA32904820801013"),
                        ["editionNumber"] = new OpenApiInteger(13),
                        ["updateNumber"] = new OpenApiInteger(0)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("104US00_CHES_TYPE1_20210630_0600"),
                        ["editionNumber"] = new OpenApiInteger(9),
                        ["updateNumber"] = new OpenApiInteger(0)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("101FR40079QWERTY"),
                        ["editionNumber"] = new OpenApiInteger(2),
                        ["updateNumber"] = new OpenApiInteger(2)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("111US00_ches_dcf8_20190703T00Z"),
                        ["editionNumber"] = new OpenApiInteger(11),
                        ["updateNumber"] = new OpenApiInteger(0)
                    },
                    new OpenApiObject
                    {
                        ["productName"] = new OpenApiString("102AR00904820801012"),
                        ["editionNumber"] = new OpenApiInteger(11),
                        ["updateNumber"] = new OpenApiInteger(0)
                    }
                };
                if (operation.RequestBody?.Content?.ContainsKey("application/json") == true)
                {
                    operation.RequestBody.Content["application/json"].Example = requestExample;
                    operation.RequestBody.Description = "A list of S-100 products with their edition and update numbers for which the Exchange Set is requested.";
                }
                var responseExample = new OpenApiObject
                {
                    ["_links"] = new OpenApiObject
                    {
                        ["exchangeSetBatchStatusUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status") },
                        ["exchangeSetBatchDetailsUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272") },
                        ["exchangeSetFileUri"] = new OpenApiObject { ["href"] = new OpenApiString("http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip") }
                    },
                    ["exchangeSetUrlExpiryDateTime"] = new OpenApiString("2021-02-17T16:19:32.269Z"),
                    ["requestedProductCount"] = new OpenApiInteger(7),
                    ["returnedProductCount"] = new OpenApiInteger(4),
                    ["requestedProductsAlreadyUpToDateCount"] = new OpenApiInteger(1),
                    ["requestedProductsNotReturned"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["productName"] = new OpenApiString("102CA32904820801013"),
                            ["reason"] = new OpenApiString("productWithdrawn")
                        },
                        new OpenApiObject
                        {
                            ["productName"] = new OpenApiString("101DE00904820801012"),
                            ["reason"] = new OpenApiString("InvalidProduct")
                        }
                    },
                    ["fssBatchId"] = new OpenApiString("7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272")
                };
                if (operation.Responses.TryGetValue("202", out var acceptedResponse) &&
                    acceptedResponse.Content?.ContainsKey("application/json") == true)
                {
                    acceptedResponse.Content["application/json"].Example = responseExample;
                }
            }
        }

        private static void AddInternalServerErrorResponse(OpenApiOperation operation)
        {            
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
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["correlationId"] = new OpenApiSchema { Type = "string" },
                                    ["detail"] = new OpenApiSchema { Type = "string" }
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}

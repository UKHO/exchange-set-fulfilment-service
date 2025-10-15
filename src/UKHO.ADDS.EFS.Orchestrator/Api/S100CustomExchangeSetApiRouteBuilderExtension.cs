using System.Net;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Any;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Api.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Generators;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Validators.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    /// <summary>
    /// Extension methods for registering S100 Exchange Set API endpoints
    /// </summary>
    internal static class S100CustomExchangeSetApiRouteBuilderExtension
    {
        /// <summary>
        /// Registers S100 Exchange Set API endpoints
        /// </summary>
        /// <param name="routeBuilder">The endpoint route builder</param>
        /// <param name="loggerFactory">The logger factory</param>
        public static void RegisterS100CustomExchangeSetApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory,ICorrelationIdGenerator correlationIdGenerator)
        {
            var logger = loggerFactory.CreateLogger("S100ExchangeSetApi");
            var exchangeSetEndpoint = routeBuilder.MapGroup("/v2/exchangeSet/s100").WithTags("s100").RequireAuthorization();

            // POST /v2/exchangeSet/s100/productNames
            exchangeSetEndpoint.MapPost("/productNames", async (
                List<string> productNamesRequest,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                IS100ProductNamesRequestValidator productNameValidator,
                string? callbackUri = null) =>
                 {
                     try
                     {
                         var correlationId = httpContext.GetCorrelationId();

                         if (productNamesRequest == null || productNamesRequest.Count == 0)
                         {
                             return BadRequestForMalformedBody(correlationId.ToString(), logger);
                         }
                         else
                         {
                             var validationResult = await productNameValidator.ValidateAsync((productNamesRequest, callbackUri));
                             var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                             if (validationResponse != null)
                             {
                                 return validationResponse;
                             }
                         }

                         var parameters = AssemblyPipelineParameters.CreateFromS100ProductNames(productNamesRequest!, configuration, (string)correlationId, callbackUri);
                         var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                         logger.LogAssemblyPipelineStarted(parameters);

                         var result = await pipeline.RunAsync(httpContext.RequestAborted);

                         // Check for errors in the response
                         if (result.ErrorResponse != null)
                         {
                             return HandleErrorResponse(result.ErrorResponse);
                         }

                         return Results.Accepted(null, result.Response);
                     }
                     catch (Exception)
                     {
                         throw;
                     }
                 })
            .Produces<CustomExchangeSetResponse>(202)
            .Produces<ErrorResponseModel>(400)
            .Produces(401)
            .Produces(403)
            .Produces(429)
            .Produces<InternalServerError>(500)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", correlationIdGenerator.CreateForCustomExchageSet().ToString())
            .WithDescription("Given a list of Product names, return all the products that are releasable.\r\n\r\nBusiness Rules:\r\nOnly Products that are releasable at the date of the request will be returned.\r\n\r\nIf valid Products are requested then Product exchange set with baseline data including requested Products will be returned.\r\n\r\nIf a requested Product has been cancelled or replaced, then the replacement Product will not be included in the response payload. Only the specific Products requested will be returned.\r\n\r\nIf none of the Products requested exist then exchange set with baseline releasable data without requested Products will be returned.")
            .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C)
            .WithOpenApi(operation =>
            {
                // Set request body example for productNamesRequest
                if (operation.RequestBody?.Content != null && operation.RequestBody.Content.ContainsKey("application/json"))
                {
                    operation.RequestBody.Content["application/json"].Example = new OpenApiArray
                    {
                        new OpenApiString("101DE14C5XN")
                    };
                }
                // Set response example for 202
                if (operation.Responses.TryGetValue("202", out var response202) && response202.Content.ContainsKey("application/json"))
                {
                    response202.Content["application/json"].Example = new OpenApiObject
                    {
                        ["links"] = new OpenApiObject
                        {
                            ["exchangeSetBatchStatusUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://example.com/batch/status/123") },
                            ["exchangeSetBatchDetailsUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://example.com/batch/details/123") },
                            ["exchangeSetFileUri"] = new OpenApiObject { ["uri"] = new OpenApiString("https://example.com/batch/file/123.zip") }
                        },
                        ["exchangeSetUrlExpiryDateTime"] = new OpenApiDateTime(DateTime.UtcNow.AddDays(7)),
                        ["requestedProductCount"] = new OpenApiInteger(2),
                        ["exchangeSetProductCount"] = new OpenApiInteger(2),
                        ["requestedProductsAlreadyUpToDateCount"] = new OpenApiInteger(0),
                        ["requestedProductsNotInExchangeSet"] = new OpenApiArray
                        {
                            new OpenApiObject
                            {
                                ["productName"] = new OpenApiString("101DE14C5XN"),
                                ["reason"] = new OpenApiString("invalidProduct")
                            }
                        },
                        ["fssBatchId"] = new OpenApiString("batch-123")
                    };
                }
                // Set response example for 400
                if (operation.Responses.TryGetValue("400", out var response400) && response400.Content.ContainsKey("application/json"))
                {
                    response400.Content["application/json"].Example = new OpenApiObject
                    {
                        ["correlationId"] = new OpenApiString("abc123-correlation-id"),
                        ["errors"] = new OpenApiArray
                        {
                            new OpenApiObject
                            {
                                ["source"] = new OpenApiString("productNames"),
                                ["description"] = new OpenApiString("One or more product names are invalid.")
                            },
                            new OpenApiObject
                            {
                                ["source"] = new OpenApiString("callbackUri"),
                                ["description"] = new OpenApiString("Callback URI is not a valid URL.")
                            }
                        }
                    };
                }
                return operation;
            });

            // POST /v2/exchangeSet/s100/productVersions
            exchangeSetEndpoint.MapPost("/productVersions", async (
                List<ProductVersionRequest> productVersionsRequest,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                IS100ProductVersionsRequestValidator productVersionsRequestValidator,
                ICorrelationIdGenerator correlationIdGenerator,
                string? callbackUri = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    if (productVersionsRequest == null || productVersionsRequest.Count == 0)
                    {
                        return BadRequestForMalformedBody(correlationId.ToString(), logger);
                    }
                    else
                    {
                        // Validate input
                        var validationResult = await productVersionsRequestValidator.ValidateAsync((productVersionsRequest, callbackUri));
                        var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                        if (validationResponse != null)
                        {
                            return validationResponse;
                        }
                    }

                    var parameters = AssemblyPipelineParameters.CreateFromS100ProductVersions(productVersionsRequest!, configuration, (string)correlationId, callbackUri);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    if (result.ErrorResponse != null)
                    {
                        return HandleErrorResponse(result.ErrorResponse);
                    }

                    return Results.Accepted(null, result.Response);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<CustomExchangeSetResponse>(202)
            .Produces<ErrorResponseModel>(400)
            .Produces(401)
            .Produces(403)
            .Produces(429)
            .Produces<InternalServerError>(500)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", correlationIdGenerator.CreateForCustomExchageSet().ToString())
            .WithDescription("Given a set of S100 Product versions (e.g. Edition x Update y) provide any later releasable files.")
            .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C);

            // POST /v2/exchangeSet/s100/updatesSince
            exchangeSetEndpoint.MapPost("/updatesSince", async (
                UpdatesSinceRequest updatesSinceRequest,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                IS100UpdateSinceRequestValidator updateSinceRequestValidator,
                ICorrelationIdGenerator correlationIdGenerator,
                string? callbackUri = null,
                string? productIdentifier = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    var validationResult = await updateSinceRequestValidator.ValidateAsync((updatesSinceRequest!, callbackUri, productIdentifier));
                    var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                    if (validationResponse != null)
                    {
                        return validationResponse;
                    }

                    var parameters = AssemblyPipelineParameters.CreateFromS100UpdatesSince(updatesSinceRequest!, configuration, (string)correlationId, productIdentifier, callbackUri);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    if (result.ErrorResponse != null)
                    {
                        return HandleErrorResponse(result.ErrorResponse);
                    }

                    return Results.Accepted(null, result.Response);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<CustomExchangeSetResponse>(202)
            .Produces(304)
            .Produces<ErrorResponseModel>(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(429)
            .Produces<InternalServerError>(500)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", correlationIdGenerator.CreateForCustomExchageSet().ToString())
            .WithDescription("Provide all the releasable S100 data after a datetime.")
            .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C);
        }

        private static IResult HandleErrorResponse(ErrorResponseModel errorResponse)
        {
            var error = errorResponse.Errors.FirstOrDefault();
            // Check for exchange set size exceeded error (case-insensitive)
            if (error != null && (string.Equals(error.Source, "exchangeSetSize", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.RequestEntityTooLarge);
            }
            
            // For other errors, return as Bad Request
            return Results.BadRequest(errorResponse);
        }

        static IResult HandleValidationResult(ValidationResult validationResult, ILogger logger, string correlationId)
        {
            if (!validationResult.IsValid)
            {
                var errorResponse = new ErrorResponseModel
                {
                    CorrelationId = correlationId,
                    Errors = validationResult.Errors
                        .Select(e => new ErrorDetail
                        {
                            Source = e.PropertyName,
                            Description = e.ErrorMessage
                        })
                        .ToList()
                };

                var validationErrors = validationResult.Errors.Select(error => $"{error.ErrorMessage}").ToList();

                logger.S100InputValidationFailed(errorResponse);

                return Results.BadRequest(errorResponse);
            }
            return null;
        }

        private static IResult BadRequestForMalformedBody(string correlationId, ILogger logger)
        {
            var errorResponse = new ErrorResponseModel
            {
                CorrelationId = correlationId.ToString(),
                Errors = new List<ErrorDetail>
                {
                    new()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                }
            };
            logger.S100InputValidationFailed(errorResponse);
            return Results.BadRequest(errorResponse);
        }
    }
}

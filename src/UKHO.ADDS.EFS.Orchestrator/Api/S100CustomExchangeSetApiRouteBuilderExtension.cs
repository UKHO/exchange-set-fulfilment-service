using FluentValidation.Results;
using Microsoft.Net.Http.Headers;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
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
        public static void RegisterS100CustomExchangeSetApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("S100ExchangeSetApi");
            var exchangeSetEndpoint = routeBuilder.MapGroup("/v2/exchangeSet/s100");

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

                         return Results.Accepted(null, result.Response);
                     }
                     catch (Exception)
                     {
                         throw;
                     }
                 })
            .Produces<CustomExchangeSetResponse>(202)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Provide all the latest releasable baseline data for a specified set of S100 Products.")
            .WithRequiredAuthorization(AuthenticationConstants.EfsRole)            
            .AddEndpointFilter<ModelBindingErrorFilter<List<string>>>();

            // POST /v2/exchangeSet/s100/productVersions
            exchangeSetEndpoint.MapPost("/productVersions", async (
                List<ProductVersionRequest> productVersionsRequest,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                IS100ProductVersionsRequestValidator productVersionsRequestValidator,
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

                    return Results.Accepted(null, result.Response);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<CustomExchangeSetResponse>(202)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Given a set of S100 Product versions (e.g. Edition x Update y) provide any later releasable files.")
            .WithRequiredAuthorization(AuthenticationConstants.EfsRole)
            .AddEndpointFilter<ModelBindingErrorFilter<List<ProductVersionRequest>>>();

            // POST /v2/exchangeSet/s100/updatesSince
            exchangeSetEndpoint.MapPost("/updatesSince", async (
                UpdatesSinceRequest updatesSinceRequest,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                IS100UpdateSinceRequestValidator updateSinceRequestValidator,
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

                    return Results.Accepted(null, result.Response);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<CustomExchangeSetResponse>(202)
            .Produces(304)
            .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID", Guid.NewGuid().ToString("N"))
            .WithDescription("Provide all the releasable S100 data after a datetime.")
            .WithRequiredAuthorization(AuthenticationConstants.EfsRole);
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

        //Rhz filter


        public sealed class ModelBindingErrorFilter<T> : IEndpointFilter where T : class
        {
            private readonly ILogger<ModelBindingErrorFilter<T>> _logger;
            private readonly bool _allowEmptyCollections;

            public ModelBindingErrorFilter(ILogger<ModelBindingErrorFilter<T>> logger,
                                           bool allowEmptyCollections = false)
            {
                _logger = logger;
                _allowEmptyCollections = allowEmptyCollections;
            }

            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                var httpContext = context.HttpContext;
                var ct = httpContext.RequestAborted;

                // Quick content-type check (optional safeguard)
                if (!HasJsonContentType(httpContext.Request))
                {
                    return BadRequest("unsupportedContentType", "Content-Type must be application/json.");
                }

                // Locate first argument of expected type T
                T? model = null;
                for (int i = 0; i < context.Arguments.Count; i++)
                {
                    if (context.Arguments[i] is T typed)
                    {
                        model = typed;
                        break;
                    }
                }

                if (model is null)
                {
                    return BadRequest("missingOrMalformedBody", "Request body is null, missing, or malformed JSON.");
                }

                // Optional: treat empty collections as invalid
                if (!_allowEmptyCollections && model is System.Collections.ICollection c && c.Count == 0)
                {
                    return BadRequest("emptyCollection", "Request body collection must contain at least one item.");
                }

                return await next(context);

                IResult BadRequest(string code, string message)
                {
                    var correlationId = httpContext.GetCorrelationId().ToString();
                    var errorResponse = new ErrorResponseModel
                    {
                        CorrelationId = correlationId,
                        Errors = new List<ErrorDetail>
            {
                new()
                {
                    Source = "requestBody",
                    Description = message
                }
            }
                    };

                    _logger.S100InputValidationFailed(errorResponse);
                    // Could also use Results.Problem(...) if adopting ProblemDetails.
                    return Results.BadRequest(errorResponse);
                }
            }

            private static bool HasJsonContentType(HttpRequest request)
            {
                if (!request.Headers.TryGetValue(HeaderNames.ContentType, out var value))
                    return false;

                // Basic check; can be expanded to handle charset, etc.
                return value.Any(v => v.StartsWith("application/json", StringComparison.OrdinalIgnoreCase));
            }
        }
        //Rhz filter end



    }
}

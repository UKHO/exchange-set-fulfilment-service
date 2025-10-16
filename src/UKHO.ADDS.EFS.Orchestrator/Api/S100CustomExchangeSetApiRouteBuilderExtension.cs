using System.Net;
using FluentValidation.Results;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Api.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers;
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
        private const string ApiLoggerName = "S100ExchangeSetApi";
        private const string BaseRoutePath = "/v2/exchangeSet/s100";
        private const string ExchangeSetSizeSource = "exchangeSetSize";
        private const string RequestBodySource = "requestBody";
        private const string MalformedBodyMessage = "Either body is null or malformed.";

        /// <summary>
        /// Registers S100 Exchange Set API endpoints
        /// </summary>
        /// <param name="routeBuilder">The endpoint route builder</param>
        /// <param name="loggerFactory">The logger factory</param>
        /// <param name="correlationIdGenerator">The correlation ID generator</param>
        /// <param name="externalApiResponseHandler">The SCS response handler</param>
        public static void RegisterS100CustomExchangeSetApi(
            this IEndpointRouteBuilder routeBuilder,
            ILoggerFactory loggerFactory,
            ICorrelationIdGenerator correlationIdGenerator,
            IExternalApiResponseHandler externalApiResponseHandler)
        {
            var logger = loggerFactory.CreateLogger(ApiLoggerName);
            var exchangeSetEndpoint = routeBuilder.MapGroup(BaseRoutePath);

            RegisterProductNamesEndpoint(exchangeSetEndpoint, logger, correlationIdGenerator, externalApiResponseHandler);
            RegisterProductVersionsEndpoint(exchangeSetEndpoint, logger, correlationIdGenerator, externalApiResponseHandler);
            RegisterUpdatesSinceEndpoint(exchangeSetEndpoint, logger, correlationIdGenerator, externalApiResponseHandler);
        }

        private static void RegisterProductNamesEndpoint(
            RouteGroupBuilder exchangeSetEndpoint,
            ILogger logger,
            ICorrelationIdGenerator correlationIdGenerator,
            IExternalApiResponseHandler externalApiResponseHandler)
        {
            exchangeSetEndpoint.MapPost("/productNames", async (
                    List<string> productNamesRequest,
                    IConfiguration configuration,
                    IAssemblyPipelineFactory pipelineFactory,
                    HttpContext httpContext,
                    IS100ProductNamesRequestValidator productNameValidator,
                    string? callbackUri = null) =>
                {
                    var correlationId = httpContext.GetCorrelationId();

                    // Validate request body
                    if (IsNullOrEmpty(productNamesRequest))
                    {
                        return CreateBadRequestForMalformedBody(correlationId.ToString(), logger);
                    }

                    // Validate input parameters
                    var validationResult = await productNameValidator.ValidateAsync((productNamesRequest, callbackUri));
                    var validationResponse = HandleValidationResult(validationResult, logger, correlationId.ToString());
                    if (validationResponse != null)
                    {
                        return validationResponse;
                    }

                    // Process request
                    return await ProcessExchangeSetRequest(
                        () => AssemblyPipelineParameters.CreateFromS100ProductNames(productNamesRequest, configuration,
                            correlationId.ToString(), callbackUri),
                        ExchangeSetType.ProductNames,
                        pipelineFactory,
                        externalApiResponseHandler,
                        logger,
                        httpContext);
                })
                .Produces<CustomExchangeSetResponse>(202)
                .Produces<ErrorResponseModel>(413)
                .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID",
                    correlationIdGenerator.CreateForCustomExchageSet().ToString())
                .WithDescription(
                    "Provide all the latest releasable baseline data for a specified set of S100 Products.")
                .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C);
        }

        private static void RegisterProductVersionsEndpoint(
            RouteGroupBuilder exchangeSetEndpoint,
            ILogger logger,
            ICorrelationIdGenerator correlationIdGenerator,
            IExternalApiResponseHandler externalApiResponseHandler)
        {
            exchangeSetEndpoint.MapPost("/productVersions", async (
                    List<ProductVersionRequest> productVersionsRequest,
                    IConfiguration configuration,
                    IAssemblyPipelineFactory pipelineFactory,
                    HttpContext httpContext,
                    IS100ProductVersionsRequestValidator productVersionsRequestValidator,
                    string? callbackUri = null) =>
                {
                    var correlationId = httpContext.GetCorrelationId();

                    // Validate request body
                    if (IsNullOrEmpty(productVersionsRequest))
                    {
                        return CreateBadRequestForMalformedBody(correlationId.ToString(), logger);
                    }

                    // Validate input parameters
                    var validationResult =
                        await productVersionsRequestValidator.ValidateAsync((productVersionsRequest, callbackUri));
                    var validationResponse = HandleValidationResult(validationResult, logger, correlationId.ToString());
                    if (validationResponse != null)
                    {
                        return validationResponse;
                    }

                    // Process request
                    return await ProcessExchangeSetRequest(
                        () => AssemblyPipelineParameters.CreateFromS100ProductVersions(productVersionsRequest,
                            configuration, correlationId.ToString(), callbackUri),
                        ExchangeSetType.ProductVersions,
                        pipelineFactory,
                        externalApiResponseHandler,
                        logger,
                        httpContext);
                })
                .Produces<CustomExchangeSetResponse>(202)
                .Produces<ErrorResponseModel>(413)
                .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID",
                    correlationIdGenerator.CreateForCustomExchageSet().ToString())
                .WithDescription(
                    "Given a set of S100 Product versions (e.g. Edition x Update y) provide any later releasable files.")
                .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C);
        }

        private static void RegisterUpdatesSinceEndpoint(
            RouteGroupBuilder exchangeSetEndpoint,
            ILogger logger,
            ICorrelationIdGenerator correlationIdGenerator,
            IExternalApiResponseHandler externalApiResponseHandler)
        {
            exchangeSetEndpoint.MapPost("/updatesSince", async (
                    UpdatesSinceRequest updatesSinceRequest,
                    IConfiguration configuration,
                    IAssemblyPipelineFactory pipelineFactory,
                    HttpContext httpContext,
                    IS100UpdateSinceRequestValidator updateSinceRequestValidator,
                    string? callbackUri = null,
                    string? productIdentifier = null) =>
                {
                    var correlationId = httpContext.GetCorrelationId();

                    // Validate input parameters (updatesSinceRequest can be null for this endpoint)
                    var validationResult =
                        await updateSinceRequestValidator.ValidateAsync((updatesSinceRequest, callbackUri,
                            productIdentifier));
                    var validationResponse = HandleValidationResult(validationResult, logger, correlationId.ToString());
                    if (validationResponse != null)
                    {
                        return validationResponse;
                    }

                    // Process request
                    return await ProcessExchangeSetRequest(
                        () => AssemblyPipelineParameters.CreateFromS100UpdatesSince(updatesSinceRequest!, configuration,
                            correlationId.ToString(), productIdentifier, callbackUri),
                        ExchangeSetType.UpdatesSince,
                        pipelineFactory,
                        externalApiResponseHandler,
                        logger,
                        httpContext);
                })
                .Produces<CustomExchangeSetResponse>(202)
                .Produces(304)
                .Produces<ErrorResponseModel>(413)
                .WithRequiredHeader(ApiHeaderKeys.XCorrelationIdHeaderKey, "Correlation ID",
                    correlationIdGenerator.CreateForCustomExchageSet().ToString())
                .WithDescription("Provide all the releasable S100 data after a datetime.")
                .WithRequiredAuthorization(AuthenticationConstants.AdOrB2C);
        }

        /// <summary>
        /// Common method to process exchange set requests, reducing code duplication
        /// </summary>
        private static async Task<IResult> ProcessExchangeSetRequest(
            Func<AssemblyPipelineParameters> createParameters,
            ExchangeSetType exchangeSetType,
            IAssemblyPipelineFactory pipelineFactory,
            IExternalApiResponseHandler externalApiResponseHandler,
            ILogger logger,
            HttpContext httpContext)
        {
            var parameters = createParameters();
            var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

            logger.LogAssemblyPipelineStarted(parameters);

            var result = await pipeline.RunAsync(httpContext.RequestAborted);

            if (result.ErrorResponse != null)
            {
                return HandleErrorResponse(result.ErrorResponse);
            }

            return externalApiResponseHandler.HandleExternalApiResponse(result, exchangeSetType.ToString(), logger, httpContext);
        }

        /// <summary>
        /// Handles error responses with improved error categorization
        /// </summary>
        private static IResult HandleErrorResponse(ErrorResponseModel errorResponse)
        {
            var error = errorResponse.Errors.FirstOrDefault();

            // Check for exchange set size exceeded error (case-insensitive)
            if (error?.Source?.Equals(ExchangeSetSizeSource, StringComparison.OrdinalIgnoreCase) == true)
            {
                return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.RequestEntityTooLarge);
            }

            // For other errors, return as Bad Request
            return Results.BadRequest(errorResponse);
        }

        /// <summary>
        /// Handles validation results with improved error mapping
        /// </summary>
        private static IResult? HandleValidationResult(ValidationResult validationResult, ILogger logger,
            string correlationId)
        {
            if (validationResult.IsValid)
            {
                return null;
            }

            var errorResponse = new ErrorResponseModel
            {
                CorrelationId = correlationId,
                Errors = validationResult.Errors
                    .Select(e => new ErrorDetail { Source = e.PropertyName, Description = e.ErrorMessage })
                    .ToList()
            };

            logger.S100InputValidationFailed(errorResponse);
            return Results.BadRequest(errorResponse);
        }

        /// <summary>
        /// Creates a standardized bad request response for malformed request bodies
        /// </summary>
        private static IResult CreateBadRequestForMalformedBody(string correlationId, ILogger logger)
        {
            var errorResponse = new ErrorResponseModel
            {
                CorrelationId = correlationId,
                Errors =
                [
                    new ErrorDetail { Source = RequestBodySource, Description = MalformedBodyMessage }
                ]
            };

            logger.S100InputValidationFailed(errorResponse);
            return Results.BadRequest(errorResponse);
        }

        /// <summary>
        /// Helper method to check if a collection is null or empty
        /// </summary>
        private static bool IsNullOrEmpty<T>(ICollection<T>? collection) => collection is null or { Count: 0 };
    }
}

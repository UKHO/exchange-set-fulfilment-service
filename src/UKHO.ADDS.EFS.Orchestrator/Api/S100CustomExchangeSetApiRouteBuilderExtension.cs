using UKHO.ADDS.EFS.Domain.Messages;
using UKHO.ADDS.EFS.Messages;
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
    public static class S100CustomExchangeSetApiRouteBuilderExtension
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
                List<string> productNames,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                S100ProductNamesRequestValidator productNameValidator,
                string? callbackUri = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    var request = new S100ProductNamesRequest
                    {
                        ProductNames = productNames,
                        CallbackUri = callbackUri
                    };

                    var validationResult = await productNameValidator.ValidateAsync(request);
                    var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                    if (validationResponse != null)
                        return validationResponse;

                    logger.S100InputValidationSucceeded((string)correlationId, RequestType.ProductNames.ToString());

                    var parameters = AssemblyPipelineParameters.CreateFromS100ProductNames(productNames, configuration, (string)correlationId, callbackUri);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    return Results.Accepted(null, result.ResponseData);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<S100CustomExchangeSetResponse>(202)
            .Produces<ErrorResponseModel>(400)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", $"job-{Guid.NewGuid():N}")
            .WithDescription("Provide all the latest releasable baseline data for a specified set of S100 Products.");

            // POST /v2/exchangeSet/s100/productVersions
            exchangeSetEndpoint.MapPost("/productVersions", async (
                List<S100ProductVersion> productVersions,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                S100ProductVersionsRequestValidator productVersionsRequestValidator,
                string? callbackUri = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    // Validate input
                    var validationResult = productVersionsRequestValidator.Validate((productVersions, callbackUri));
                    var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                    if (validationResponse != null)
                        return validationResponse;

                    logger.S100InputValidationSucceeded((string)correlationId, RequestType.ProductVersions.ToString());

                    var parameters = AssemblyPipelineParameters.CreateFromS100ProductVersions(productVersions, configuration, (string)correlationId, callbackUri);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    return Results.Accepted(null, result.ResponseData);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<S100CustomExchangeSetResponse>(202)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", $"job-{Guid.NewGuid():N}")
            .WithDescription("Given a set of S100 Product versions (e.g. Edition x Update y) provide any later releasable files.");

            // POST /v2/exchangeSet/s100/updatesSince
            exchangeSetEndpoint.MapPost("/updatesSince", async (
                S100UpdatesSinceRequest request,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                S100UpdateSinceRequestValidator updateSinceRequestValidator,
                string? callbackUri = null,
                string? productIdentifier = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    var validationResult = await updateSinceRequestValidator.ValidateAsync((request, callbackUri, productIdentifier));
                    var validationResponse = HandleValidationResult(validationResult, logger, (string)correlationId);
                    if (validationResponse != null)
                        return validationResponse;

                    logger.S100InputValidationSucceeded((string)correlationId, RequestType.UpdatesSince.ToString());

                    var parameters = AssemblyPipelineParameters.CreateFromS100UpdatesSince(request, configuration, (string)correlationId, productIdentifier, callbackUri);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    return Results.Accepted(null, result.ResponseData);
                }
                catch (Exception)
                {
                    throw;
                }
            })
            .Produces<S100CustomExchangeSetResponse>(202)
            .Produces(304)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", $"job-{Guid.NewGuid():N}")
            .WithDescription("Provide all the releasable S100 data after a datetime.");

            static IResult HandleValidationResult(FluentValidation.Results.ValidationResult validationResult, ILogger logger, string correlationId)
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

                    logger.S100InputValidationFailed(
                        correlationId,
                        string.Join("; ", validationErrors));

                    return Results.BadRequest(errorResponse);
                }
                return null;
            }
        }
    }
}

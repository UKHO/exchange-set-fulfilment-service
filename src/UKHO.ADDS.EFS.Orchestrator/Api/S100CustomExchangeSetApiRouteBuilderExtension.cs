using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

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
                S100ProductNamesRequest request,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                string? callbackUri = null) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    var parameters = AssemblyPipelineParameters.CreateFromS100ProductNames(request, configuration, correlationId);
                    var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                    logger.LogAssemblyPipelineStarted(parameters);

                    var result = await pipeline.RunAsync(httpContext.RequestAborted);

                    // Check if there are validation errors
                    if (result.ErrorResponse?.Errors?.Count > 0)
                    {
                        return Results.BadRequest(result.ErrorResponse);
                    }

                    // Return success response
                    // Replace all instances of Results.Ok(result.ResponseData) with Results.Accepted(null, result.ResponseData)

                    return Results.Accepted(null, result.ResponseData);
                }
                catch (Exception)
                {
                    // Exception handling will be done by middleware
                    throw;
                }
            })
            .Produces<S100CustomExchangeSetResponse>(202)
            .Produces<ErrorResponseModel>(400)
            .WithRequiredHeader("x-correlation-id", "Correlation ID", $"job-{Guid.NewGuid():N}")
            .WithDescription("Provide all the latest releasable baseline data for a specified set of S100 Products.");

            // POST /v2/exchangeSet/s100/productVersions
            exchangeSetEndpoint.MapPost("/productVersions", async (
                S100ProductVersionsRequest request,
                IConfiguration configuration,
                IAssemblyPipelineFactory pipelineFactory,
                HttpContext httpContext,
                string? callbackUri = null) =>
            {
                try
                {
                    // Placeholder: endpoint not yet implemented. Return 202 Accepted with no body.
                    return Results.Accepted();
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
                string? callbackUri = null,
                string? productIdentifier = null) =>
            {
                try
                {
                    // Placeholder: endpoint not yet implemented. Return 202 Accepted with no body.
                    return Results.Accepted();
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
        }
    }
}

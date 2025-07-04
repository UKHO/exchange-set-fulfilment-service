using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class RequestsApiRouteBuilderExtension
    {
        public static void RegisterRequestsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("RequestsApi");
            var requestsEndpoint = routeBuilder.MapGroup("/requests");

            requestsEndpoint.MapPost("/", async (JobRequestApiMessage message, IConfiguration configuration, AssemblyPipelineFactory pipelineFactory, HttpContext httpContext) =>
                {
                    try
                    {
                        var correlationId = httpContext.GetCorrelationId();

                        var parameters = AssemblyPipelineParameters.CreateFrom(message, configuration, correlationId);
                        var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                        logger.LogAssemblyPipelineStarted(parameters);

                        var result = await pipeline.RunAsync(httpContext.RequestAborted);

                        return result;
                    }
                    catch (Exception e)
                    {
                        logger.LogAssemblyPipelineFailed(message, e);
                        throw;
                    }
                })
                .Produces<AssemblyPipelineResponse>()
                .WithRequiredHeader("x-correlation-id", "Correlation ID", "a-correlation-id")
                .WithDescription("Create a job request for the given data standard (currently only supports S100)");
        }
    }
}

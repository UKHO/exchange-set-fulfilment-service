using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        public static void RegisterJobsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("JobsApi");
            var jobsEndpoint = routeBuilder.MapGroup("/jobs");

            jobsEndpoint.MapPost("/", async (JobRequestApiMessage message, IConfiguration configuration, IAssemblyPipelineFactory pipelineFactory, HttpContext httpContext) =>
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
                .WithRequiredHeader("x-correlation-id", "Correlation ID", $"job-{Guid.NewGuid():N}")
                .WithDescription("Create a job request for the given data standard. To filter (S100) by product type, use the filter property \"startswith(ProductName, '101')\"");

            jobsEndpoint.MapGet("/{jobId}", async (string jobId, ITable<Job> jobTable) =>
            {
                var jobResult = await jobTable.GetUniqueAsync(jobId);

                if (jobResult.IsSuccess(out var job))
                {
                    return Results.Ok(job);
                }

                logger.LogGetJobRequestFailed(jobId);
                return Results.NotFound();
            }).WithDescription("Gets the job for the given job id");

            jobsEndpoint.MapGet("/{jobId}/build", async (string jobId, ITable<BuildMemento> mementoTable) =>
            {
                var mementoResult = await mementoTable.GetUniqueAsync(jobId);

                if (mementoResult.IsSuccess(out var memento))
                {
                    return Results.Ok(memento);
                }

                logger.LogGetJobRequestFailed(jobId);
                return Results.NotFound();
            }).WithDescription("Gets the job build memento for the given job id");

            //jobsEndpoint.MapGet("/{jobId}/status", async (string jobId, ITable<BuildStatus> table) =>
            //    {
            //        var statusResult = await table.GetUniqueAsync(jobId);

            //        if (statusResult.IsSuccess(out var status))
            //        {
            //            return Results.Ok(status);
            //        }

            //        return Results.NotFound();
            //    })
            //    .WithDescription("Gets the build status for the given job request");
        }
    }
}

using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        public static void RegisterJobsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("JobsApi");
            var jobsEndpoint = routeBuilder.MapGroup("/jobs");

            jobsEndpoint.MapGet("/{jobId}", async (string jobId, S100ExchangeSetJobTable table) =>
            {
                var jobResult = await table.GetAsync(jobId, jobId);

                if (jobResult.IsSuccess(out var job))
                {
                    return Results.Ok(job);
                }

                logger.LogGetJobRequestFailed(jobId);

                return Results.NotFound();
            });

            jobsEndpoint.MapGet("/{jobId}/status", async (string jobId, ExchangeSetBuildStatusTable table) =>
            {
                var statusResult = await table.GetAsync(jobId, jobId);

                if (statusResult.IsSuccess(out var status))
                {
                    return Results.Ok(status);
                }

                return Results.NotFound();
            });
        }
    }
}

using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        public static void RegisterJobsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("JobsApi");
            var jobsEndpoint = routeBuilder.MapGroup("/jobs");

            jobsEndpoint.MapGet("/{jobId}", async (string jobId, ITable<JobTable> jobTable) =>
            {
                var jobResult = await jobTable.GetUniqueAsync(jobId);

                if (jobResult.IsSuccess(out var job))
                {
                    return Results.Ok(job);
                }

                logger.LogGetJobRequestFailed(jobId);
                return Results.NotFound();
            }).WithDescription("Gets the job details for the given job request");

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

using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Messages;
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

            jobsEndpoint.MapGet("/{jobId}", async (string jobId, ITable<ExchangeSetJobType> jobTypeTable, ITable<S100ExchangeSetJob> s100JobTable, ITable<S63ExchangeSetJob> s63JobTable, ITable<S57ExchangeSetJob> s57JobTable) =>
                {
                    var jobTypeResult = await jobTypeTable.GetUniqueAsync(jobId);

                    if (!jobTypeResult.IsSuccess(out var jobType))
                    {
                        return Results.NotFound();
                    }

                    switch (jobType.DataStandard)
                    {
                        case ExchangeSetDataStandard.S100:
                            var s100JobResult = await s100JobTable.GetUniqueAsync(jobId);

                            if (s100JobResult.IsSuccess(out var s100Job))
                            {
                                return Results.Ok(s100Job);
                            }

                            break;
                        case ExchangeSetDataStandard.S63:
                            var s63JobResult = await s63JobTable.GetUniqueAsync(jobId);

                            if (s63JobResult.IsSuccess(out var s63Job))
                            {
                                return Results.Ok(s63Job);
                            }

                            break;
                        case ExchangeSetDataStandard.S57:
                            var s57JobResult = await s57JobTable.GetUniqueAsync(jobId);

                            if (s57JobResult.IsSuccess(out var s57Job))
                            {
                                return Results.Ok(s57Job);
                            }

                            break;
                        default:
                            logger.LogGetJobRequestFailed(jobId);
                            return Results.NotFound();
                    }

                    return Results.NotFound();
                })
                .WithDescription("Gets the job details for the given job request");

            jobsEndpoint.MapGet("/{jobId}/status", async (string jobId, ITable<BuildStatus> table) =>
                {
                    var statusResult = await table.GetUniqueAsync(jobId);

                    if (statusResult.IsSuccess(out var status))
                    {
                        return Results.Ok(status);
                    }

                    return Results.NotFound();
                })
                .WithDescription("Gets the build status for the given job request");
        }
    }
}

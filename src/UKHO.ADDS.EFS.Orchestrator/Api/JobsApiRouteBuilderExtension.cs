﻿using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        // TODO This is still S100-specific, refactor

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
                })
                .WithDescription("Gets the job details for the given job request");

            jobsEndpoint.MapGet("/{jobId}/status", async (string jobId, BuildStatusTable table) =>
                {
                    var statusResult = await table.GetAsync(jobId, jobId);

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

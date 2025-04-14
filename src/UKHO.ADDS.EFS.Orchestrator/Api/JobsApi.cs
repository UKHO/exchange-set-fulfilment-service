using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class JobsApi
    {
        public static void Register(WebApplication application)
        {
            application.MapGet("/jobs", async (ExchangeSetJobTable table) =>
            {
                var requests = await table.ListAsync();
                return Results.Ok(requests);
            });

            application.MapGet("/jobs/{jobId}", async (string jobId, ExchangeSetJobTable table, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("JobsApi");

                var jobResult = await table.GetAsync(jobId, jobId);

                if (jobResult.IsSuccess(out var job))
                {
                    logger.LogInformation("Job {jobId} requested by builder", jobId);

                    return Results.Ok(job);
                }

                return Results.NotFound();
            });

#if DEBUG
            // Used by the builder in a debug session to send a debug job (created by the builder for testing) to the orchestrator
            application.MapPost("/jobs/debug/{jobId}", async (string jobId, ExchangeSetJob job, ExchangeSetJobTable table, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("JobsApi");

                await table.CreateIfNotExistsAsync();
                await table.AddAsync(job);

                logger.LogInformation("Received debug build request : {jobId}", jobId);
            });
#endif
        }
    }
}

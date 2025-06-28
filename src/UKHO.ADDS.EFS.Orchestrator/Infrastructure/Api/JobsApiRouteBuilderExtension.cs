using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        public static void RegisterJobsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("JobsApi");
            var jobsEndpoint = routeBuilder.MapGroup("/jobs");

            jobsEndpoint.MapGet("/", async (S100ExchangeSetJobTable table) =>
            {
                var requests = await table.ListAsync();
                return Results.Ok(requests);
            });

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

#if DEBUG
            //// Used by the builder in a debug session to send a debug job (created by the builder for testing) to the orchestrator
            //jobsEndpoint.MapPost("/debug/", async (ExchangeSetJob job, S100ExchangeSetJobTable table) =>
            //{
            //    await table.AddAsync(job);
            //});
#endif
        }
    }
}

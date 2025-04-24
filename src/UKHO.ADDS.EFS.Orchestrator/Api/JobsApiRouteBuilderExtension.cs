using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Extensions;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class JobsApiRouteBuilderExtension
    {
        public static void RegisterJobsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("JobsApi");
            var jobsEndpoint = routeBuilder.MapGroup("/jobs");

            jobsEndpoint.MapGet("/", async (ExchangeSetJobTable table) =>
            {
                var requests = await table.ListAsync();
                return Results.Ok(requests);
            });

            jobsEndpoint.MapGet("/{jobId}", async (string jobId, ExchangeSetJobTable table, HttpContext httpContext) =>
            {
                var correlationId = httpContext.GetCorrelationId();

                var jobResult = await table.GetAsync(jobId, jobId);

                if (jobResult.IsSuccess(out var job))
                {
                    logger.LogInformation("Job {jobId} requested by builder | Correlation ID: {_X-Correlation-ID}", jobId, correlationId);

                    return Results.Ok(job);
                }

                return Results.NotFound();
            });

#if DEBUG
            // Used by the builder in a debug session to send a debug job (created by the builder for testing) to the orchestrator
            jobsEndpoint.MapPost("/debug/{jobId}", async (string jobId, ExchangeSetJob job, ExchangeSetJobTable table, HttpContext httpContext) =>
            {
                var correlationId = httpContext.GetCorrelationId();

                await table.CreateIfNotExistsAsync();
                await table.AddAsync(job);

                logger.LogInformation("Received debug build request : {jobId} | Correlation ID: {_X-Correlation-ID}", jobId, correlationId);
            });
#endif
        }
    }
}

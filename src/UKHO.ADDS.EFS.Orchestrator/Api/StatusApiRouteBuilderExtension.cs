using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class StatusApiRouteBuilderExtension
    {
        public static void RegisterStatusApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("StatusApi");
            var statusEndpoint = routeBuilder.MapGroup("/status");

            statusEndpoint.MapPost("/", async (ExchangeSetBuilderNodeStatus status, ExchangeSetBuilderNodeStatusTable table, HttpContext httpContext) =>
            {
                var correlationId = httpContext.GetCorrelationId();

                await table.CreateIfNotExistsAsync();
                await table.AddAsync(status);

                logger.LogInformation("Received builder node status update : {status.JobId} -> {status.NodeId} | Correlation ID: {_X-Correlation-ID}", status.JobId, status.NodeId, correlationId);
            });

            statusEndpoint.MapGet("/{jobId}", async (string jobId, ExchangeSetBuilderNodeStatusTable table, HttpContext httpContext) =>
            {
                var correlationId = httpContext.GetCorrelationId();

                var statuses = await table.GetAsync(jobId);

                logger.LogInformation("Received status request for job {jobId} | Correlation ID: {_X-Correlation-ID}", jobId, correlationId);

                return Results.Ok(statuses);
            });
        }
    }
}

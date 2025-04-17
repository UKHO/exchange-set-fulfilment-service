using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class StatusApi
    {
        public static void Register(WebApplication application)
        {
            application.MapPost("/status", async (ExchangeSetBuilderNodeStatus status, ExchangeSetBuilderNodeStatusTable table, HttpContext httpContext, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("StatusApi");

                var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;

                await table.CreateIfNotExistsAsync();
                await table.AddAsync(status);

                logger.LogInformation("Received builder node status update : {status.JobId} -> {status.NodeId} | Correlation ID: {_X-Correlation-ID}", status.JobId, status.NodeId, correlationId);
            });

            application.MapGet("/status/{jobId}", async (string jobId, ExchangeSetBuilderNodeStatusTable table, HttpContext httpContext, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("StatusApi");

                var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;

                var statuses = await table.GetAsync(jobId);

                logger.LogInformation("Received status request for job {jobId} | Correlation ID: {_X-Correlation-ID}", jobId, correlationId);

                return Results.Ok(statuses);
            });
        }
    }
}

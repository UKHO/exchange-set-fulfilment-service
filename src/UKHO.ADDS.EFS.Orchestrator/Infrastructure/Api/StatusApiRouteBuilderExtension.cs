using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Api
{
    public static class StatusApiRouteBuilderExtension
    {
        public static void RegisterStatusApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("StatusApi");
            var statusEndpoint = routeBuilder.MapGroup("/status");

            statusEndpoint.MapPost("/", async (BuildNodeStatus status, ExchangeSetBuilderNodeStatusTable table) =>
            {
                try
                {
                    await table.AddAsync(status);
                }
                catch (Exception e)
                {
                    logger.LogPostedStatusUpdateFromBuilderFailed(status, e);
                    throw;
                }
            });

            statusEndpoint.MapGet("/{jobId}", async (string jobId, ExchangeSetBuilderNodeStatusTable table) =>
            {
                var statuses = await table.GetAsync(jobId);

                return Results.Ok(statuses);
            });
        }
    }
}

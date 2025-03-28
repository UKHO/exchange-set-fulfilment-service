using Serilog;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class StatusApi
    {
        public static void Register(WebApplication application)
        {
            application.MapPost("/status", async (ExchangeSetBuilderNodeStatus status, ExchangeSetBuilderNodeStatusTable table) =>
            {
                await table.CreateTableIfNotExistsAsync();
                await table.AddAsync(status);

                Log.Information($"Received builder node status update : {status.RequestId} -> {status.NodeId}");
            });

            application.MapGet("/status/{requestId}", async (string requestId, ExchangeSetBuilderNodeStatusTable table) =>
            {
                var statuses = await table.GetAsync(requestId);
                return Results.Ok(statuses);
            });
        }
    }
}

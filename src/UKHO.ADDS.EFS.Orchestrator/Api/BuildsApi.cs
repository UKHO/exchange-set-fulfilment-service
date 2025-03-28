using Azure.Storage.Queues;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class BuildsApi
    {
        public static void Register(WebApplication application)
        {
            application.MapGet("/builds", async (ExchangeSetRequestTable table) =>
            {
                var requests = await table.ListAsync();
                return Results.Ok(requests);
            });

            application.MapGet("/builds/{id}/request", async (string id, ExchangeSetRequestTable table) =>
            {
                var requestResult = await table.GetAsync(id, id);

                if (requestResult.IsSuccess(out var request))
                {
                    return Results.Ok(request);
                }

                return Results.NotFound();
            });

            application.MapPost("/builds", (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient) =>
            {
                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                queueClient.SendMessage(JsonCodec.Encode(message));
            });

#if DEBUG
            application.MapPost("/builds/debug/{id}", async (string id, ExchangeSetRequest request, ExchangeSetRequestTable table) =>
            {
                await table.CreateTableIfNotExistsAsync();
                await table.AddAsync(request);

                Log.Information($"Received debug build request : {id}");
            });
#endif
        }
    }
}

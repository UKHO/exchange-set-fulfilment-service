using Azure.Storage.Queues;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class RequestsApi
    {
        public static void Register(WebApplication application) =>
            application.MapPost("/requests", (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient) =>
            {
                var messageJson = JsonCodec.Encode(message);

                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                queueClient.SendMessage(messageJson);

                Log.Information($"Received request : {messageJson}");
            });
    }
}

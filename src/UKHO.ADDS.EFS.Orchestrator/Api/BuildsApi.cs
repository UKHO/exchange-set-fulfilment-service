using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Common.Configuration.Namespaces;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class BuildsApi
    {
        public static void Register(WebApplication application)
        {
            application.MapGet("/builds", (HttpContext context) =>
            {

            });

            application.MapPost("/builds", (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient) =>
            {
                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                queueClient.SendMessage(JsonCodec.Encode(message));
            });
        }
    }
}

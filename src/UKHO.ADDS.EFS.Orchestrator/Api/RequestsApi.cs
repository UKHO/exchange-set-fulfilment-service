using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    internal static class RequestsApi
    {
        public static void Register(WebApplication application) =>
            application.MapPost("/requests", async (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient, HttpContext httpContext, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("RequestsApi");

                var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;

                // Set correlation ID in the message
                //message.Id = correlationId;

                var messageJson = JsonCodec.Encode(message);

                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                await queueClient.SendMessageAsync(messageJson);

                logger.LogInformation("Received request: {MessageJson}", messageJson);
            });
    }
}

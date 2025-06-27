using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Api.Metadata;
using UKHO.ADDS.EFS.Orchestrator.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class RequestsApiRouteBuilderExtension
    {
        public static void RegisterRequestsApi(this IEndpointRouteBuilder routeBuilder, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("RequestsApi");
            var requestsEndpoint = routeBuilder.MapGroup("/requests");

            requestsEndpoint.MapPost("/", async (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient, HttpContext httpContext) =>
            {
                try
                {
                    var correlationId = httpContext.GetCorrelationId();

                    var queueMessage = new ExchangeSetRequestQueueMessage { DataStandard = message.DataStandard, Products = message.Products, CorrelationId = correlationId };

                    var messageJson = JsonCodec.Encode(queueMessage);

                    var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.JobRequestQueueName);
                    await queueClient.SendMessageAsync(messageJson);

                    logger.LogPostedExchangeSetQueueMessage(queueMessage);
                }
                catch (Exception e)
                {
                    logger.LogPostedExchangeSetQueueFailedMessage(message, e);
                    throw;
                }

            }).WithRequiredHeader("x-correlation-id", "Correlation ID", "a-correlation-id");
        }
    }
}

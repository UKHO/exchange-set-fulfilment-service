using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Extensions;
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
                var zz = new { Property1 = "a prop", Property2 = "another prop" };

                logger.LogInformation("Received request: {@zz}", zz);


                var correlationId = httpContext.GetCorrelationId();

                message.CorrelationId = correlationId;

                var messageJson = JsonCodec.Encode(message);

                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                await queueClient.SendMessageAsync(messageJson);

                logger.LogInformation("Received request: {MessageJson} | Correlation ID: {_X-Correlation-ID}", messageJson, correlationId);
            });
        }
    }
}

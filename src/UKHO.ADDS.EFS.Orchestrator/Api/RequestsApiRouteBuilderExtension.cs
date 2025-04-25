using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Constants;
using static System.Net.Mime.MediaTypeNames;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public static class RequestsApiRouteBuilderExtension
    {
        public static void RegisterRequestsApi(this IEndpointRouteBuilder routeBuilder)
        {
            var requestsEndpoint = routeBuilder.MapGroup("/requests");

            requestsEndpoint.MapPost("/", async (ExchangeSetRequestMessage message, QueueServiceClient queueServiceClient, HttpContext httpContext, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("RequestsApi");

                var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;

                message.CorrelationId = correlationId;

                var messageJson = JsonCodec.Encode(message);

                var queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);
                await queueClient.SendMessageAsync(messageJson);

                logger.LogInformation("Received request: {MessageJson} | Correlation ID: {X-Correlation-ID}", messageJson, correlationId);
            });
        }
    }
}

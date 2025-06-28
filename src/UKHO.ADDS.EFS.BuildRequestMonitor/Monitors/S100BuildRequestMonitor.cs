using Azure.Storage.Queues;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Monitors
{
    internal class S100BuildRequestMonitor : BackgroundService
    {
        private readonly ILogger<S100BuildRequestMonitor> _logger;
        private readonly QueueServiceClient _queueClient;
        private readonly S100BuildRequestProcessor _processor;

        public S100BuildRequestMonitor(ILogger<S100BuildRequestMonitor> logger, QueueServiceClient qClient, S100BuildRequestProcessor processor)
        {
            _logger = logger;
            _queueClient = qClient;
            _processor = processor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var s100BuildRequestQueue = _queueClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);
            await s100BuildRequestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var messages = await s100BuildRequestQueue.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout: TimeSpan.FromMinutes(5), cancellationToken: stoppingToken);
                    foreach (var message in messages.Value)
                    {
                        var request = JsonCodec.Decode<BuildRequest>(message.MessageText)!;

                        switch (request.DataStandard)
                        {
                            case ExchangeSetDataStandard.S100:
                                _logger.LogInformation("Received S100 build request for JobId: {JobId}, BatchId: {BatchId}", request.JobId, request.BatchId);

                                await _processor.ProcessRequestAsync(request, cancellationToken: stoppingToken);
                                break;
                            default:
                                _logger.LogWarning("Received unsupported data standard: {DataStandard} for JobId: {JobId}, BatchId: {BatchId}", request.DataStandard, request.JobId, request.BatchId);

                                await s100BuildRequestQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);
                                break;
                        }

                        // We do not delete the message here, as it will be handled by the builder that processes the request
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing messages from the queue.");
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before checking for new messages
            }
        }
    }
}

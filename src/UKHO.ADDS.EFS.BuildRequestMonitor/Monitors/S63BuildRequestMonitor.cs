using Azure.Storage.Queues;
using UKHO.ADDS.EFS.BuildRequestMonitor.Builders;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Monitors
{
    internal class S63BuildRequestMonitor : BackgroundService
    {
        private readonly ILogger<S63BuildRequestMonitor> _logger;
        private readonly QueueServiceClient _queueClient;
        private readonly S63BuildRequestProcessor _processor;

        private readonly List<JobId> _processedJobs;

        public S63BuildRequestMonitor(ILogger<S63BuildRequestMonitor> logger, QueueServiceClient qClient, S63BuildRequestProcessor processor)
        {
            _logger = logger;
            _queueClient = qClient;
            _processor = processor;

            _processedJobs = [];
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var s63BuildRequestQueue = _queueClient.GetQueueClient(StorageConfiguration.S63BuildRequestQueueName);
            var s63BuildResponseQueue = _queueClient.GetQueueClient(StorageConfiguration.S63BuildResponseQueueName);

            await ConfigureQueue(s63BuildRequestQueue, stoppingToken);
            await ConfigureQueue(s63BuildResponseQueue, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var peekedMessages  = await s63BuildRequestQueue.PeekMessagesAsync(maxMessages: 10, cancellationToken: stoppingToken);

                    if (peekedMessages.Value.Length == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); 
                        continue;
                    }

                    foreach (var message in peekedMessages.Value)
                    {
                        try
                        {
                            var request = JsonCodec.Decode<S63BuildRequest>(message.MessageText)!;

                            if (_processedJobs.Contains(request.JobId))
                            {
                                continue;
                            }

                            _processedJobs.Add(request.JobId);

                            switch (request.DataStandard)
                            {
                                case DataStandard.S63:
                                    _logger.LogInformation("Received S63 build request for JobId: {JobId}, BatchId: {BatchId}", request.JobId, request.BatchId);

                                    await _processor.ProcessRequestAsync(request, cancellationToken: stoppingToken);
                                    break;
                                default:
                                    _logger.LogWarning("Received unsupported data standard: {DataStandard} for JobId: {JobId}, BatchId: {BatchId}", request.DataStandard, request.JobId, request.BatchId);

                                    await DeleteMessage(s63BuildRequestQueue);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An error occurred while processing the message {message.MessageText}");

                            // Message is malformed in some way, so delete it
                            await DeleteMessage(s63BuildRequestQueue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing messages from the queue.");
                }
            }
        }

        private async Task ConfigureQueue(QueueClient client, CancellationToken stoppingToken)
        {
            await client.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            // Ensure we have mopped up any messages from the last run
            await client.ClearMessagesAsync(cancellationToken: stoppingToken);
        }

        private async Task DeleteMessage(QueueClient client)
        {
            var received = await client.ReceiveMessageAsync();
            await client.DeleteMessageAsync(received.Value.MessageId, received.Value.PopReceipt);
        }
    }
}

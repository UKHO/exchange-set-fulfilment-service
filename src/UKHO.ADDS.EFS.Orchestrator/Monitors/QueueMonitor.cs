using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal abstract class QueueMonitor<T> : BackgroundService where T : class
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly int _pollingIntervalSeconds;
        private readonly int _queueBatchSize;
        private readonly QueueClient _queueClient;

        protected QueueMonitor(string queueName, string pollingIntervalSecondsKey, string queueBatchSizeKey, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;

            _queueClient = queueServiceClient.GetQueueClient(queueName);

            _pollingIntervalSeconds = configuration.GetValue<int>(pollingIntervalSecondsKey);
            _queueBatchSize = configuration.GetValue<int>(queueBatchSizeKey);
        }

        protected IConfiguration Configuration => _configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] queueMessages = await _queueClient.ReceiveMessagesAsync(_queueBatchSize, cancellationToken: stoppingToken);

                    foreach (var message in queueMessages)
                    {
                        var messageInstance = JsonCodec.Decode<T>(message.MessageText)!;
                        await ProcessMessageAsync(messageInstance, stoppingToken);

                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogQueueServiceMessageReadFailed(GetType().Name, ex);

                    // TODO: Dead letter, remove...
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Task was cancelled, exit the loop
                    break;
                }
            }
        }

        protected abstract Task ProcessMessageAsync(T messageInstance, CancellationToken stoppingToken);
    }
}

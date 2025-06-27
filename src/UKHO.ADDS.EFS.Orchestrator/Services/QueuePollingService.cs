using System.Threading.Channels;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class QueuePollingService : BackgroundService
    {
        private readonly Channel<ExchangeSetRequestQueueMessage> _channel;
        private readonly ILogger<QueuePollingService> _logger;

        private readonly int _pollingIntervalSeconds;
        private readonly int _queueBatchSize;
        private readonly QueueClient _queueClient;

        public QueuePollingService(Channel<ExchangeSetRequestQueueMessage> channel, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<QueuePollingService> logger)
        {
            _channel = channel;
            _logger = logger;
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.JobRequestQueueName);

            _pollingIntervalSeconds = configuration.GetValue<int>("JobRequestQueue:PollingIntervalSeconds");
            _queueBatchSize = configuration.GetValue<int>("JobRequestQueue:BatchSize");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] queueMessages = await _queueClient.ReceiveMessagesAsync(_queueBatchSize, cancellationToken: stoppingToken);

                    foreach (var message in queueMessages)
                    {
                        await _channel.Writer.WriteAsync(JsonCodec.Decode<ExchangeSetRequestQueueMessage>(message.MessageText)!, stoppingToken);
                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogQueueServiceMessageReadFailed(ex);

                    // TODO: Dead letter, remove...
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }

            _channel.Writer.Complete();
        }
    }
}

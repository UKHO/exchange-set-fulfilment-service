using System.Threading.Channels;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class QueuePollingService : BackgroundService
    {
        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly ILogger<QueuePollingService> _logger;

        private readonly int _pollingIntervalSeconds;
        private readonly int _queueBatchSize;
        private readonly QueueClient _queueClient;

        public QueuePollingService(Channel<ExchangeSetRequestMessage> channel, QueueServiceClient queueServiceClient, IConfiguration configuration, ILogger<QueuePollingService> logger)
        {
            _channel = channel;
            _logger = logger;
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);

            _pollingIntervalSeconds = configuration.GetValue<int>("QueuePolling:PollingIntervalSeconds");
            _queueBatchSize = configuration.GetValue<int>("QueuePolling:BatchSize");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(_queueBatchSize, cancellationToken: stoppingToken);

                    foreach (var message in messages)
                    {
                        var exchangeSetRequestMessage = JsonCodec.Decode<ExchangeSetRequestMessage>(message.MessageText)!;

                        await _channel.Writer.WriteAsync(JsonCodec.Decode<ExchangeSetRequestMessage>(message.MessageText)!, stoppingToken);

                        _logger.LogInformation("Message with ID: {MessageId} written to the channel. | Correlation ID: {_X-Correlation-ID}", message.MessageId, exchangeSetRequestMessage.CorrelationId);

                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading message in queue polling service");

                    // TODO: Dead letter, remove...
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }

            _channel.Writer.Complete();
        }
    }
}

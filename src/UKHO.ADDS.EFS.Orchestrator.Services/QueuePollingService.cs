using System.Threading.Channels;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class QueuePollingService : BackgroundService
    {
        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly QueueClient _queueClient;

        private readonly int _pollingIntervalSeconds;

        public QueuePollingService(Channel<ExchangeSetRequestMessage> channel, QueueServiceClient queueServiceClient, IConfiguration configuration)
        {
            _channel = channel;
            _queueClient = queueServiceClient.GetQueueClient(StorageConfiguration.RequestQueueName);

            _pollingIntervalSeconds = configuration.GetValue<int>("QueuePolling:PollingIntervalSeconds");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    QueueMessage[] messages = await _queueClient.ReceiveMessagesAsync(maxMessages: 25, cancellationToken: stoppingToken);

                    foreach (var message in messages)
                    {
                        await _channel.Writer.WriteAsync(JsonCodec.Decode<ExchangeSetRequestMessage>(message.MessageText)!, stoppingToken);

                        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error reading message in queue polling service");

                    // TODO: Dead letter, remove...
                }

                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }

            _channel.Writer.Complete();
        }
    }
}

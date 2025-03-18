using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;  

namespace ESSFulfilmentService.Orchestrator
{
    public sealed class Worker (
        QueueServiceClient queueClient,
        ServiceBusClient busClient,
        ILogger<Worker> logger) : BackgroundService
    {
        

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var requestQueue = queueClient.GetQueueClient("requestqueue");
            await requestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

            var sender = busClient.CreateSender("iic-topic");

            while (!stoppingToken.IsCancellationRequested)
            {
                QueueMessage message = await requestQueue.ReceiveMessageAsync(cancellationToken: stoppingToken);
                if (message != null)
                {
                    logger.LogInformation("Received message from Queue: {0}", message.Body.ToString());
                    await sender.SendMessageAsync(new ServiceBusMessage(message.Body));
                    await requestQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken: stoppingToken);
                }

                
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace UKHO.ADDS.EFS.Builder.S100
{
    public static class QueueWaiter
    {
        public static async Task<QueueMessage> WaitForSingleMessageAsync(
            QueueClient queueClient,
            TimeSpan pollInterval,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Waiting for a message...");

            while (!cancellationToken.IsCancellationRequested)
            {
                QueueMessage[] messages = (await queueClient.ReceiveMessagesAsync(1, cancellationToken:cancellationToken)).Value;

                if (messages.Length > 0)
                {
                    Console.WriteLine("Message received.");
                    return messages[0];
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            throw new OperationCanceledException("Message wait was cancelled.");
        }
    }
}

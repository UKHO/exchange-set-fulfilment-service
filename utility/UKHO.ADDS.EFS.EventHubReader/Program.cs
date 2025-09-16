using System.Text;
using Azure.Messaging.EventHubs.Consumer;

namespace UKHO.ADDS.EFS.EventHubReader
{

    internal class Program
    {
        private static readonly string _connectionString = "";  // TODO: Add event hub connection string
        private static readonly string _consumerGroup = "";  // TODO: Add consumer group
        private static readonly string _eventHubLogFolder = @"D:\EventHubLogs";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Event Hub Log Reader started...");

            EnsureLogDirectoryExists();
            var logFilePath = GetLogFilePath();

            await using var consumer = new EventHubConsumerClient(_consumerGroup, _connectionString);
            var fromTime = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancellation requested...");
                e.Cancel = true;
                cts.Cancel();
            };

            await ProcessPartitionsAsync(consumer, fromTime, logFilePath, cts.Token);

            Console.WriteLine($"Logs written to: {logFilePath}");
            Console.WriteLine("Event Hub Log Reader stopped.");
        }

        private static void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(_eventHubLogFolder))
            {
                Directory.CreateDirectory(_eventHubLogFolder);
                Console.WriteLine($"Created log directory: {_eventHubLogFolder}");
            }
        }

        private static string GetLogFilePath()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss");
            return Path.Combine(_eventHubLogFolder, $"EFS_EventHub_Logs_{timestamp}.txt");
        }

        private static async Task ProcessPartitionsAsync(EventHubConsumerClient consumer,DateTimeOffset fromTime,
            string logFilePath,CancellationToken cancellationToken)
        {
            var partitionIds = await consumer.GetPartitionIdsAsync(cancellationToken);
            Console.WriteLine($"Found {partitionIds.Length} partitions.");

            foreach (var partitionId in partitionIds)
            {
                Console.WriteLine($"Processing partition: {partitionId}");

                await foreach (var partitionEvent in consumer.ReadEventsFromPartitionAsync(partitionId,
                    EventPosition.FromEnqueuedTime(fromTime), cancellationToken))
                {
                    if (partitionEvent.Data != null)
                    {
                        var eventBody = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                        var logEntry = $"{partitionEvent.Data.EnqueuedTime.LocalDateTime}: {eventBody}\n\n";

                        Console.WriteLine(logEntry);
                        await File.AppendAllTextAsync(logFilePath, logEntry, cancellationToken);
                    }
                }
            }
        }
    }
}



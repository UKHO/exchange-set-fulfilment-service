using System.Text;
using Azure.Messaging.EventHubs.Consumer;

namespace UKHO.ADDS.EFS.EventHubReader
{
    // NOTE: This file is for local testing only and should not be committed to source control.
    internal static class Program
    {
        private static readonly string _connectionString = "";  // Add event hub connection string
        private static readonly string _consumerGroup = "";   // Add consumer group
        private static readonly string _eventHubLogFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "EventHubLogs");

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Event Hub Log Reader started...");

            EnsureLogDirectoryExists();
            var logFilePath = GetLogFilePath();

            await using var consumer = new EventHubConsumerClient(_consumerGroup, _connectionString);
            var fromTime = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);

            using var cts = new CancellationTokenSource();
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

        private static async Task ProcessPartitionsAsync(EventHubConsumerClient consumer, DateTimeOffset fromTime,
            string logFilePath, CancellationToken cancellationToken)
        {
            var partitionIds = await consumer.GetPartitionIdsAsync(cancellationToken);
            Console.WriteLine($"Found {partitionIds.Length} partitions.");

            foreach (var partitionId in partitionIds)
            {
                Console.WriteLine($"Processing partition: {partitionId}");

                var events = consumer.ReadEventsFromPartitionAsync(partitionId,
                    EventPosition.FromEnqueuedTime(fromTime), cancellationToken);

                await foreach (var partitionEvent in events)
                {
                    var eventData = partitionEvent.Data;
                    if (eventData != null)
                    {
                        var eventBody = Encoding.UTF8.GetString(eventData.Body.ToArray());
                        var logEntry = $"{eventData.EnqueuedTime.LocalDateTime}: {eventBody}\n\n";

                        Console.WriteLine(logEntry);
                        await File.AppendAllTextAsync(logFilePath, logEntry, cancellationToken);
                    }
                }
            }
        }
    }
}



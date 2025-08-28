using System.Text;
using System.Threading.Channels;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    /// <summary>
    /// A high-throughput, non-blocking Serilog sink that publishes structured logs to Azure Event Hub using
    /// managed identity authentication and adaptive batching.
    /// </summary>
    public class EventHubSerilogSink : ILogEventSink, IAsyncDisposable
    {
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ITextFormatter _textFormatter;
        private readonly Channel<LogEvent> _logChannel;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _processingTask;

        // Configuration Parameters

        // Maximum number of events allowed per flush batch (as per Event Hub constraints)
        private const int MaxBatchSize = 200;

        // Size of the in-memory bounded channel buffer; controls how many log events can be queued
        private const int LogChannelSize = 3000;

        // Periodic flush interval for the background processing loop
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

        // Timeout applied to each EventHub send operation to avoid hanging on network issues
        private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

        // Percentage threshold to trigger adaptive flush based on buffer fullness
        private const double AdaptiveFlushThreshold = 0.8;

        /// <summary>
        /// Constructs a new instance of the Event Hub sink.
        /// </summary>
        /// <param name="connectionString">Event Hub fully qualified namespace (FQDN) from env by default.</param>
        /// <param name="formatter">Optional Serilog formatter. Defaults to JsonFormatter.</param>
        public EventHubSerilogSink(string? connectionString = null, ITextFormatter? formatter = null)
        {
            // Read Event Hub namespace (FQDN) and hub name from environment
            connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__efs-events-namespace");
            var eventHubName = ServiceConfiguration.EventHubName;

            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(eventHubName))
                throw new InvalidOperationException("Missing Event Hub connection string or event hub name.");

            // Use the provided or default JSON formatter
            _textFormatter = formatter ?? new JsonFormatter();

            // Instantiate EventHub client with DefaultAzureCredential (MSI-based auth) and WebSocket transport
            _eventHubProducerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: connectionString,
                eventHubName: eventHubName,
                credential: new DefaultAzureCredential(),
                clientOptions: new EventHubProducerClientOptions
                {
                    ConnectionOptions = new EventHubConnectionOptions
                    {
                        TransportType = EventHubsTransportType.AmqpWebSockets // supports firewall/proxy environments
                    }
                });

            // Create a bounded channel for non-blocking log event ingestion
            _logChannel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(LogChannelSize)
            {
                FullMode = BoundedChannelFullMode.DropWrite, // Drop logs if buffer is full (avoids backpressure blocking)
                SingleReader = true,
                SingleWriter = false
            });

            // Start background task to consume and flush logs from the channel
            _processingTask = Task.Run(ProcessLogQueueAsync);
        }

        /// <summary>
        /// Pushes a log event into the internal channel. Drops if the buffer is full.
        /// </summary>
        public void Emit(LogEvent logEvent)
        {
            if (!_logChannel.Writer.TryWrite(logEvent))
            {
                Console.Error.WriteLine("Log buffer full. Dropping log.");
            }
        }

        /// <summary>
        /// Background log processing loop. Batches events and flushes periodically or when threshold is met.
        /// </summary>
        private async Task ProcessLogQueueAsync()
        {
            var buffer = new List<EventData>();
            var timer = new PeriodicTimer(FlushInterval);

            try
            {
                while (await _logChannel.Reader.WaitToReadAsync(_cancellationTokenSource.Token))
                {
                    while (_logChannel.Reader.TryRead(out var logEvent))
                    {
                        using var sw = new StringWriter();
                        _textFormatter.Format(logEvent, sw);
                        var payload = Encoding.UTF8.GetBytes(sw.ToString());
                        buffer.Add(new EventData(payload));

                        // Trigger flush early if we're nearing the max batch size
                        if (buffer.Count >= MaxBatchSize * AdaptiveFlushThreshold)
                        {
                            await FlushAsync(buffer);
                        }
                    }

                    // Trigger timed flush
                    if (await timer.WaitForNextTickAsync(_cancellationTokenSource.Token) && buffer.Count > 0)
                    {
                        await FlushAsync(buffer);
                    }
                }
            }
            catch (OperationCanceledException) { /* Expected during shutdown */ }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Log processing error: {ex}");
            }
            finally
            {
                // Ensure logs are flushed even if cancellation occurs
                if (buffer.Count > 0)
                {
                    await FlushAsync(buffer);
                }
            }
        }

        /// <summary>
        /// Flushes the current buffer of events to Event Hub, splitting into batches as needed.
        /// </summary>
        private async Task FlushAsync(List<EventData> buffer)
        {
            try
            {
                EventDataBatch currentBatch = await _eventHubProducerClient.CreateBatchAsync(_cancellationTokenSource.Token);

                foreach (var evt in buffer)
                {
                    // If current batch is full, send and start new batch
                    if (!currentBatch.TryAdd(evt))
                    {
                        await SafeSendAsync(currentBatch);
                        currentBatch.Dispose();

                        currentBatch = await _eventHubProducerClient.CreateBatchAsync(_cancellationTokenSource.Token);

                        // If event is too large to fit in an empty batch, drop it
                        if (!currentBatch.TryAdd(evt))
                        {
                            Console.Error.WriteLine("Single log too large to send.");
                        }
                    }
                }

                // Final send if anything remains
                if (currentBatch.Count > 0)
                {
                    await SafeSendAsync(currentBatch);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Flush failed: {ex}");
            }
            finally
            {
                buffer.Clear(); // Reset buffer after flush
            }
        }

        /// <summary>
        /// Wraps Event Hub send with timeout enforcement to avoid unresponsive hangs.
        /// </summary>
        private async Task SafeSendAsync(EventDataBatch batch)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            timeoutCts.CancelAfter(SendTimeout);

            try
            {
                await _eventHubProducerClient.SendAsync(batch, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SendAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanly shuts down the background processor and flushes all pending logs.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                _cancellationTokenSource.Cancel(); // Signal cancellation
                _logChannel.Writer.TryComplete(); // Complete channel
                await _processingTask; // Wait for background loop to finish
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DisposeAsync error: {ex}");
            }
            finally
            {
                await _eventHubProducerClient.DisposeAsync(); // Cleanup Event Hub client
            }
        }
    }
}

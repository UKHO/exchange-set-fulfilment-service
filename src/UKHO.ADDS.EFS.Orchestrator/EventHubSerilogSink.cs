using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Threading.Channels;
using System.Net.Sockets;
using Azure.Identity;

public class EventHubSerilogSink : ILogEventSink, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly ITextFormatter _formatter;
    private readonly Channel<LogEvent> _logChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    // Configuration
    private const int MaxBatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

    public EventHubSerilogSink(string? connectionString = null, ITextFormatter? formatter = null)
    {
        connectionString ??= Environment.GetEnvironmentVariable("ConnectionStrings__efs-events-namespace");
        var eventHubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME");
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(eventHubName))
            throw new InvalidOperationException("Missing Event Hub connection string or event hub name.");

        _formatter = formatter ?? new JsonFormatter();

         _producerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: connectionString,
                eventHubName: eventHubName,
                credential: new DefaultAzureCredential(),
                clientOptions: new EventHubProducerClientOptions
                {
                    ConnectionOptions = new EventHubConnectionOptions
                    {
                        TransportType = EventHubsTransportType.AmqpWebSockets
                    }
                }
            );

        _logChannel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

        _processingTask = Task.Run(ProcessLogQueueAsync);
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_logChannel.Writer.TryWrite(logEvent))
        {
            Console.Error.WriteLine("Log buffer full. Dropping log.");
        }
    }

    private async Task ProcessLogQueueAsync()
    {
        var buffer = new List<EventData>();
        var timer = new PeriodicTimer(FlushInterval);

        try
        {
            while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_logChannel.Reader.TryRead(out var logEvent))
                {
                    using var sw = new StringWriter();
                    _formatter.Format(logEvent, sw);
                    var payload = Encoding.UTF8.GetBytes(sw.ToString());
                    buffer.Add(new EventData(payload));

                    if (buffer.Count >= MaxBatchSize)
                        await FlushAsync(buffer);
                }

                if (await timer.WaitForNextTickAsync(_cts.Token) && buffer.Count > 0)
                    await FlushAsync(buffer);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Log processing error: {ex}");
        }
        finally
        {
            if (buffer.Count > 0)
                await FlushAsync(buffer);
        }
    }

    private async Task FlushAsync(List<EventData> buffer)
    {
        try
        {
            using var batch = await _producerClient.CreateBatchAsync(_cts.Token);
            foreach (var evt in buffer)
            {
                if (!batch.TryAdd(evt))
                {
                    await SafeSendAsync(batch);
                    using var newBatch = await _producerClient.CreateBatchAsync(_cts.Token);
                    if (!newBatch.TryAdd(evt))
                        Console.Error.WriteLine("Single log too large to send.");
                    else
                        await SafeSendAsync(newBatch);
                }
            }
            await SafeSendAsync(batch);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Flush failed: {ex}");
        }
        finally
        {
            buffer.Clear();
        }
    }

    private async Task SafeSendAsync(EventDataBatch batch)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        timeoutCts.CancelAfter(SendTimeout);

        try
        {
            await _producerClient.SendAsync(batch, timeoutCts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SendAsync failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
            _logChannel.Writer.TryComplete();
            await _processingTask;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DisposeAsync error: {ex}");
        }
        finally
        {
            await _producerClient.DisposeAsync();
        }
    }
}

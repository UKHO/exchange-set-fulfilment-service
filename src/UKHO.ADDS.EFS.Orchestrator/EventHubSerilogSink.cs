using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Threading.Channels;
using System.Net.Http;

public class EventHubSerilogSink : ILogEventSink, IDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly ITextFormatter _formatter;
    private readonly Channel<LogEvent> _logChannel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;

    public EventHubSerilogSink(ITextFormatter? formatter = null)
    {
        _formatter = formatter ?? new JsonFormatter();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__efs-events-namespace");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Environment variable 'ConnectionStrings__efs-events-namespace' is not set.");
        }
        Console.WriteLine($"Using Event Hub connection string: {connectionString}");
        var clientOptions = new EventHubProducerClientOptions
        {
            ConnectionOptions = new EventHubConnectionOptions
            {
                TransportType = EventHubsTransportType.AmqpWebSockets
            }
        };

        _producerClient = new EventHubProducerClient(connectionString, clientOptions);

        _logChannel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

        _cts = new CancellationTokenSource();
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
        try
        {
            while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_logChannel.Reader.TryRead(out var logEvent))
                {
                    using var sw = new StringWriter();
                    _formatter.Format(logEvent, sw);
                    var jsonLog = sw.ToString();
                    var eventData = new EventData(Encoding.UTF8.GetBytes(jsonLog));

                    try
                    {
                        using EventDataBatch batch = await _producerClient.CreateBatchAsync();
                        if (!batch.TryAdd(eventData))
                        {
                            Console.Error.WriteLine("Log event too large for Event Hub batch.");
                            continue;
                        }

                        await _producerClient.SendAsync(batch);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to send log to Event Hub: {ex}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Log processing crashed: {ex}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _logChannel.Writer.Complete();
        try
        {
            _processingTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during log flushing: {ex}");
        }

        _producerClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Basic connectivity test to verify outbound HTTPS to Event Hub namespace.
    /// This is a diagnostics tool and not part of log transport.
    /// </summary>
    public static async Task TestOutboundHttpsAsync(string fullyQualifiedNamespace)
    {
        try
        {
            var url = fullyQualifiedNamespace;
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var response = await client.GetAsync(url);
            Console.WriteLine($"Connection status for {url}: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed outbound HTTPS test to {fullyQualifiedNamespace}: {ex.Message}");
        }
    }
}
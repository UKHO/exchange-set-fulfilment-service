using System.Threading.Channels;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    public class AppInsightsSerilogSink : ILogEventSink, IAsyncDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly TelemetryConfiguration _telemetryConfig;
        private readonly ITextFormatter _formatter;
        private readonly Channel<LogEvent> _logChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _processingTask;

        private const int LogChannelSize = 3000;
        private const int MaxBatchSize = 100;
        private static readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
        private const double AdaptiveFlushThreshold = 0.8;

        public AppInsightsSerilogSink(string? connectionString = null, ITextFormatter? formatter = null)
        {
            connectionString ??= "InstrumentationKey=c1f7b0b1-3fcb-4ce6-9e02-ffeeb6834261;IngestionEndpoint=https://eastus2-3.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus2.livediagnostics.monitor.azure.com/;ApplicationId=8e904598-6eab-4c08-90e5-77f0e35492e2";
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Missing Application Insights connection string.");

            _telemetryConfig = new TelemetryConfiguration
            {
                ConnectionString = connectionString
            };

            _telemetryClient = new TelemetryClient(_telemetryConfig);
            _formatter = formatter ?? new JsonFormatter();

            _logChannel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(LogChannelSize)
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
            var buffer = new List<string>();
            var timer = new PeriodicTimer(_flushInterval);

            try
            {
                while (await _logChannel.Reader.WaitToReadAsync(_cts.Token))
                {
                    while (_logChannel.Reader.TryRead(out var logEvent))
                    {
                        using var sw = new StringWriter();
                        _formatter.Format(logEvent, sw);
                        buffer.Add(sw.ToString());

                        if (buffer.Count >= (int)(MaxBatchSize * AdaptiveFlushThreshold))
                        {
                            await FlushAsync(buffer);
                        }
                    }

                    if (await timer.WaitForNextTickAsync(_cts.Token) && buffer.Count > 0)
                    {
                        await FlushAsync(buffer);
                    }
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
                {
                    await FlushAsync(buffer);
                }
            }
        }

        private Task FlushAsync(List<string> buffer)
        {
            try
            {
                foreach (var item in buffer)
                {
                    _telemetryClient.TrackTrace(item);
                }

                _telemetryClient.Flush();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Flush failed: {ex}");
            }
            finally
            {
                buffer.Clear();
            }

            return Task.CompletedTask;
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
                _telemetryClient.Flush();
                await Task.Delay(5000);
                _telemetryConfig.Dispose();
            }
        }
    }
}

using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog
{
    public class EventHubBatchedSink : IBatchedLogEventSink, IDisposable
    {
        private readonly EventHubSink _innerSink;
        private bool _disposed;

        public EventHubBatchedSink(IEventHubLog eventHubLog, string environment, string system, string service, Action<IDictionary<string, object>> additionalValuesProvider)
        {
            _innerSink = new EventHubSink(eventHubLog, environment, system, service, additionalValuesProvider);
        }

        public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            foreach (var logEvent in batch)
            {
                _innerSink.Emit(logEvent);
            }
            return Task.CompletedTask;
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _innerSink?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EventHubBatchedSink()
        {
            Dispose(false);
        }
    }
}

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventHubs;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    internal class EventHubLog : IEventHubLog
    {
        private const int LogSerializationExceptionEventId = 7437;
        private const string LogSerializationExceptionEventName = "LogSerializationException";

        private IEventHubClientWrapper _eventHubClientWrapper;

        private readonly JsonSerializerOptions _settings;
        private readonly JsonSerializerOptions _errorSettings;
        private bool _disposed;

        public EventHubLog(IEventHubClientWrapper eventHubClientWrapper, IEnumerable<JsonConverter> customConverters)
        {
            this._eventHubClientWrapper = eventHubClientWrapper;

            // Common options
            var settings = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Add custom converters if needed
            foreach (var converter in customConverters)
            {
                settings.Converters.Add(converter);
            }

            var errorSettings = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Add custom converters if needed
            foreach (var converter in customConverters)
            {
                errorSettings.Converters.Add(converter);
            }
        }

        public async void Log(LogEntry logEntry)
        {
            try
            {
                string jsonLogEntry;
                try
                {
                    jsonLogEntry = JsonCodec.Encode(logEntry, _settings);
                }
                catch (Exception e)
                {
                    logEntry = new LogEntry
                    {
                        Exception = e,
                        Level = "Warning",
                        MessageTemplate = "Log Serialization failed with exception",
                        Timestamp = DateTime.UtcNow,
                        EventId = new EventId(LogSerializationExceptionEventId, LogSerializationExceptionEventName)
                    };
                    jsonLogEntry = JsonCodec.Encode(logEntry, _errorSettings);
                }

                await _eventHubClientWrapper.SendAsync(new EventData(Encoding.UTF8.GetBytes(jsonLogEntry)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _eventHubClientWrapper?.Dispose();
            }

            _disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EventHubLog()
        {
            Dispose(false);
        }
    }
}

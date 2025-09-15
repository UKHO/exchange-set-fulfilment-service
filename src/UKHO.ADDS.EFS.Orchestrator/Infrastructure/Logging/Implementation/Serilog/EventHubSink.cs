using Serilog.Core;
using Serilog.Events;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog
{
    public class EventHubSink : ILogEventSink, IDisposable
    {
        private readonly IEventHubLog _eventHubLog;
        private readonly string _environment;
        private readonly string _system;
        private readonly string _service;
        private readonly string _nodeName;
        private readonly Action<IDictionary<string, object>> _additionalValuesProvider;
        private bool _disposed;

        public EventHubSink(IEventHubLog eventHubLog, string environment, string system, string service, string nodeName,
            Action<IDictionary<string, object>> additionalValuesProvider)
        {
            _eventHubLog = eventHubLog ?? throw new ArgumentNullException(nameof(eventHubLog));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _nodeName = nodeName ?? throw new ArgumentNullException(nameof(nodeName));
            _additionalValuesProvider = additionalValuesProvider ?? (d => { });
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            // Convert LogLevel
            var logLevel = ConvertSerilogLevelToMicrosoftLevel(logEvent.Level);

            // Create properties dictionary with standard values
            var properties = new Dictionary<string, object>
            {
                { EventHubConstant.Environment, _environment },
                { EventHubConstant.System, _system },
                { EventHubConstant.Service, _service },
                { EventHubConstant.NodeName, _nodeName },
                { EventHubConstant.ComponentName, logEvent.Properties.ContainsKey("SourceContext")
                                   ? logEvent.Properties["SourceContext"].ToString().Trim('"')
                                   : "Unknown" }
            };

            // Add additional values if configured
            try
            {
                _additionalValuesProvider(properties);
            }
            catch (Exception e)
            {
                properties["LoggingError"] = $"additionalValuesProvider throw exception: {e.Message}";
                properties["LoggingErrorException"] = e;
            }

            // Add Serilog properties
            foreach (var property in logEvent.Properties)
            {
                var propertyName = property.Key.StartsWith("@") ? property.Key.Substring(1) : property.Key;
                if (!properties.ContainsKey(propertyName))
                {
                    properties.Add(propertyName, RenderPropertyValue(property.Value));
                }
            }

            // Create log entry
            var logEntry = new LogEntry
            {
                Timestamp = logEvent.Timestamp.UtcDateTime,
                Level = logEvent.Level.ToString(),
                MessageTemplate = logEvent.MessageTemplate.Text,
                LogProperties = properties,
                Exception = logEvent.Exception
            };

            // Send to EventHub
            _eventHubLog.Log(logEntry);
        }

        private static LogLevel ConvertSerilogLevelToMicrosoftLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => LogLevel.Trace,
                LogEventLevel.Debug => LogLevel.Debug,
                LogEventLevel.Information => LogLevel.Information,
                LogEventLevel.Warning => LogLevel.Warning,
                LogEventLevel.Error => LogLevel.Error,
                LogEventLevel.Fatal => LogLevel.Critical,
                _ => LogLevel.None
            };
        }

        private static object RenderPropertyValue(LogEventPropertyValue propertyValue)
        {
            if (propertyValue is ScalarValue scalarValue)
            {
                return scalarValue.Value;
            }

            if (propertyValue is SequenceValue sequenceValue)
            {
                return sequenceValue.Elements.Select(RenderPropertyValue).ToArray();
            }

            if (propertyValue is StructureValue structureValue)
            {
                var result = new Dictionary<string, object>();
                foreach (var property in structureValue.Properties)
                {
                    result[property.Name] = RenderPropertyValue(property.Value);
                }
                return result;
            }

            if (propertyValue is DictionaryValue dictionaryValue)
            {
                var result = new Dictionary<string, object>();
                foreach (var kvp in dictionaryValue.Elements)
                {
                    var key = RenderPropertyValue(kvp.Key).ToString();
                    result[key] = RenderPropertyValue(kvp.Value);
                }
                return result;
            }

            using var writer = new StringWriter();
            propertyValue.Render(writer);
            return writer.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _eventHubLog?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EventHubSink()
        {
            Dispose(false);
        }
    }
}

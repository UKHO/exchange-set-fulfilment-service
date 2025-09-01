// British Crown Copyright © 2024,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.Serilog
{
    public class EventHubSink : ILogEventSink, IDisposable
    {
        private readonly IEventHubLog _eventHubLog;
        private readonly string _environment;
        private readonly string _system;
        private readonly string _service;
        private readonly string _nodeName;
        private readonly Action<IDictionary<string, object>> additionalValuesProvider;
        private readonly ITextFormatter _formatter;

        public EventHubSink(
            IEventHubLog eventHubLog,
            string environment,
            string system,
            string service,
            string nodeName,
            Action<IDictionary<string, object>> additionalValuesProvider = null,
            ITextFormatter formatter = null)
        {
            this._eventHubLog = eventHubLog ?? throw new ArgumentNullException(nameof(eventHubLog));
            this._environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this._system = system ?? throw new ArgumentNullException(nameof(system));
            this._service = service ?? throw new ArgumentNullException(nameof(service));
            this._nodeName = nodeName ?? throw new ArgumentNullException(nameof(nodeName));
            this.additionalValuesProvider = additionalValuesProvider ?? (d => { });
            this._formatter = formatter ?? new JsonFormatter();
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            // Convert LogLevel
            var logLevel = ConvertSerilogLevelToMicrosoftLevel(logEvent.Level);

            // Create properties dictionary with standard values
            var properties = new Dictionary<string, object>
            {
                { "_Environment", _environment },
                { "_System", _system },
                { "_Service", _service },
                { "_NodeName", _nodeName },
                { "_ComponentName", logEvent.Properties.ContainsKey("SourceContext")
                                   ? logEvent.Properties["SourceContext"].ToString().Trim('"')
                                   : "Unknown" }
            };

            // Add additional values if configured
            try
            {
                additionalValuesProvider(properties);
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
            // Using if-else instead of switch expression for C# 7.3 compatibility
            if (level == LogEventLevel.Verbose) return LogLevel.Trace;
            if (level == LogEventLevel.Debug) return LogLevel.Debug;
            if (level == LogEventLevel.Information) return LogLevel.Information;
            if (level == LogEventLevel.Warning) return LogLevel.Warning;
            if (level == LogEventLevel.Error) return LogLevel.Error;
            if (level == LogEventLevel.Fatal) return LogLevel.Critical;
            return LogLevel.None;
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

            // Fallback to string rendering - using traditional using statement for C# 7.3 compatibility
            StringWriter writer = null;
            try
            {
                writer = new StringWriter();
                propertyValue.Render(writer);
                return writer.ToString();
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }

        public void Dispose()
        {
            _eventHubLog?.Dispose();
        }
    }
}

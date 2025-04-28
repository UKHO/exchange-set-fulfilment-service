using System.Text.Json;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Logging
{
    internal class LogForwarder
    {
        private readonly ILogger _logger;
        private readonly ExchangeSetJob _job;
        private readonly string _containerName;

        private readonly JsonObjectAggregator _aggregator;

        public LogForwarder(ILogger logger, ExchangeSetJob job, string containerName)
        {
            _logger = logger;
            _job = job;
            _containerName = containerName;

            _aggregator = new JsonObjectAggregator();
        }

        public void ForwardLog(LogLevel logLevel, string logLine)
        {
            foreach (var completedLine in _aggregator.Append(logLine.AsSpan()))
            {
                WriteLog(logLevel, completedLine);
                _aggregator.Reset();
            }
        }

        private void WriteLog(LogLevel logLevel, string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine))
            {
                return;
            }

            Dictionary<string, object>? parsedLog;

            try
            {
                parsedLog = JsonCodec.Decode<Dictionary<string, object>>(logLine);
            }
            catch (JsonException ex)
            {
                _logger.LogForwarderParseFailure(logLine, ex);
                return;
            }

            if (parsedLog is null)
            {
                _logger.LogForwarderParseNull(logLine);
                return;
            }

            var mergedLog = new Dictionary<string, object>(parsedLog)
            {
                ["server.name"] = _containerName,
                ["job.id"] = _job.Id
            };

            var logMessage = "(no message template)";

            if (parsedLog.TryGetValue("MessageTemplate", out var messageTemplate) && messageTemplate is JsonElement messageTemplateElement)
            {
                if (messageTemplateElement.ValueKind == JsonValueKind.String)
                {
                    logMessage = messageTemplateElement.GetString() ?? "(no message template)";
                }
            }
            else if (parsedLog.TryGetValue("message", out var fallbackMessage) && fallbackMessage is JsonElement { ValueKind: JsonValueKind.String } fallbackMessageElement)
            {
                logMessage = fallbackMessageElement.GetString() ?? "(no message)";
            }

            // Flatten the dictionary into structured log properties
            using (_logger.BeginScope(mergedLog))
            {
#pragma warning disable LOG001
                _logger.Log(logLevel, $"Job {_job.Id}: {logMessage}");
#pragma warning restore LOG001
            }
        }
    }
}

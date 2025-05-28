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

        /// <summary>
        /// Initializes a new instance of the LogForwarder class with the specified logger, job, and container name.
        /// </summary>
        /// <param name="logger">The logger to use for forwarding logs.</param>
        /// <param name="job">The job associated with the logs.</param>
        /// <param name="containerName">The name of the container/server for log enrichment.</param>
        public LogForwarder(ILogger logger, ExchangeSetJob job, string containerName)
        {
            _logger = logger;
            _job = job;
            _containerName = containerName;

            _aggregator = new JsonObjectAggregator();
        }

        /// <summary>
        /// Processes an incoming log line, aggregates it if necessary, and forwards completed log entries for structured logging.
        /// </summary>
        /// <param name="logLevel">The default log level to use if not specified in the log line.</param>
        /// <param name="logLine">The log line to process and forward.</param>
        public void ForwardLog(LogLevel logLevel, string logLine)
        {
            foreach (var completedLine in _aggregator.Append(logLine.AsSpan()))
            {
                WriteLog(logLevel, completedLine);
                _aggregator.Reset();
            }
        }

        /// <summary>
        /// Parses a log line, enriches it with job and container information, determines the log level, and writes it using structured logging.
        /// </summary>
        /// <param name="logLevel">The default log level to use if not specified in the log line.</param>
        /// <param name="logLine">The log line to parse and write.</param>
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

            var effectiveLogLevel = DetermineLogLevel(parsedLog, logLevel);

            // Flatten the dictionary into structured log properties
            using (_logger.BeginScope(mergedLog))
            {
#pragma warning disable LOG001
                _logger.Log(effectiveLogLevel, $"Job {_job.Id}: {logMessage}");
#pragma warning restore LOG001
            }
        }

        /// <summary>
        /// Extracts and maps the log level from the parsed log entry, falling back to the provided default if not found.
        /// </summary>
        /// <param name="parsedLog">The parsed log entry as a dictionary.</param>
        /// <param name="defaultLevel">The default log level to use if not found in the log entry.</param>
        /// <returns>The determined log level.</returns>
        private LogLevel DetermineLogLevel(Dictionary<string, object> parsedLog, LogLevel defaultLevel)
        {
            if (parsedLog.TryGetValue("Level", out var levelValue) &&
                levelValue is JsonElement levelElement &&
                levelElement.ValueKind == JsonValueKind.String)
            {
                var levelString = levelElement.GetString();
                if (!string.IsNullOrEmpty(levelString))
                {
                    if (Enum.TryParse<LogLevel>(levelString, true, out var parsedLevel))
                    {
                        return parsedLevel;
                    }

                    switch (levelString.ToLowerInvariant())
                    {
                        case "fatal":
                            return LogLevel.Critical;
                        case "error":
                            return LogLevel.Error;
                        case "warn":
                        case "warning":
                            return LogLevel.Warning;
                        case "info":
                        case "information":
                            return LogLevel.Information;
                        case "debug":
                            return LogLevel.Debug;
                        case "trace":
                            return LogLevel.Trace;
                    }
                }
            }
            return defaultLevel;
        }
    }
}

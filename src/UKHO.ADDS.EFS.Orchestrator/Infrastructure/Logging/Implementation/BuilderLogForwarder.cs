using System.Text.Json;
using Serilog.Context;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    internal class BuilderLogForwarder : IBuilderLogForwarder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly LogLevel _replayLevel;

        public BuilderLogForwarder(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _replayLevel = configuration.GetValue("Builders:LogReplayLevel", LogLevel.Information);
        }

        public async Task ForwardLogsAsync(IEnumerable<string> messages, DataStandard dataStandard, JobId jobId)
        {
            var builderName = $"Builder-{dataStandard}-{jobId}";
            var logger = _loggerFactory.CreateLogger(builderName);

            foreach (var log in messages)
            {
                WriteLog(log, logger, builderName, jobId.ToString());
                await Task.Yield();
            }
        }

        private void WriteLog(string log, ILogger logger, string builderName, string jobId)
        {
            if (string.IsNullOrWhiteSpace(log))
                return;

            Dictionary<string, object>? parsedLog;
            try
            {
                parsedLog = JsonCodec.Decode<Dictionary<string, object>>(log);
            }
            catch (JsonException)
            {
                return;
            }
            if (parsedLog is null)
                return;

            var correlationId = ExtractCorrelationId(parsedLog, jobId);
            var mergedLog = new Dictionary<string, object>(parsedLog) { ["server.name"] = builderName };
            var logMessage = ExtractLogMessage(parsedLog);

            var effectiveLogLevel = DetermineLogLevel(parsedLog, _replayLevel);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (logger.BeginScope(mergedLog))
            {
#pragma warning disable LOG001
                logger.Log(effectiveLogLevel, $"{builderName}: {logMessage}");
#pragma warning restore LOG001
            }
        }

        /// <summary>
        ///     Extracts and maps the log level from the parsed log entry, falling back to the provided default if not found.
        /// </summary>
        /// <param name="parsedLog">The parsed log entry as a dictionary.</param>
        /// <param name="defaultLevel">The default log level to use if not found in the log entry.</param>
        /// <returns>The determined log level.</returns>
        private static LogLevel DetermineLogLevel(Dictionary<string, object> parsedLog, LogLevel defaultLevel)
        {
            if (parsedLog.TryGetValue("Level", out var levelValue) && levelValue is JsonElement { ValueKind: JsonValueKind.String } levelElement)
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

        /// <summary>
        /// Extracts correlation ID from the nested Properties JSON string or falls back to jobId
        /// </summary>
        private static string ExtractCorrelationId(Dictionary<string, object> parsedLog, string fallbackJobId)
        {
            // First try to get correlation ID from top-level properties
            if (parsedLog.TryGetValue("CorrelationId", out var topLevelCorrelationId) &&
                topLevelCorrelationId is JsonElement { ValueKind: JsonValueKind.String } topLevelElement)
            {
                var correlationId = topLevelElement.GetString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    return correlationId;
                }
            }

            // Try to extract from nested Properties JSON string
            if (parsedLog.TryGetValue("Properties", out var propertiesValue) &&
                propertiesValue is JsonElement { ValueKind: JsonValueKind.String } propertiesElement)
            {
                var propertiesJson = propertiesElement.GetString();
                if (!string.IsNullOrEmpty(propertiesJson))
                {
                    try
                    {
                        var nestedProperties = JsonCodec.Decode<Dictionary<string, object>>(propertiesJson);
                        if (nestedProperties?.TryGetValue("CorrelationId", out var nestedCorrelationId) == true &&
                            nestedCorrelationId is JsonElement { ValueKind: JsonValueKind.String } nestedElement)
                        {
                            var correlationId = nestedElement.GetString();
                            if (!string.IsNullOrEmpty(correlationId))
                            {
                                return correlationId;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Failed to parse nested properties, continue with fallback
                    }
                }
            }

            // Fallback to job ID
            return fallbackJobId;
        }

        /// <summary>
        /// Extracts the log message from the parsed log entry dictionary
        /// </summary>
        /// <param name="parsedLog">The parsed log entry as a dictionary.<</param>
        /// <returns></returns>
        private static string ExtractLogMessage(Dictionary<string, object> parsedLog)
        {
            if (parsedLog.TryGetValue("MessageTemplate", out var messageTemplate) &&
                messageTemplate is JsonElement messageTemplateElement &&
                messageTemplateElement.ValueKind == JsonValueKind.String)
            {
                return messageTemplateElement.GetString() ?? "(no message template)";
            }
            if (parsedLog.TryGetValue("message", out var fallbackMessage) &&
                fallbackMessage is JsonElement fallbackMessageElement &&
                fallbackMessageElement.ValueKind == JsonValueKind.String)
            {
                return fallbackMessageElement.GetString() ?? "(no message)";
            }
            return "(no message template)";
        }
    }
}

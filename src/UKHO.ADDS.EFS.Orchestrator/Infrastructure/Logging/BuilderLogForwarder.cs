using System.Text.Json;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class BuilderLogForwarder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly LogLevel _replayLevel;


        public BuilderLogForwarder(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _replayLevel = configuration.GetValue("Builders:LogReplayLevel", LogLevel.Information);
        }

        public void ForwardLogs(IEnumerable<string> messages, ExchangeSetDataStandard dataStandard, string jobId)
        {
            var builderName = $"Builder-{dataStandard}-{jobId}";
            var logger = _loggerFactory.CreateLogger(builderName);

            foreach (var log in messages)
            {
                WriteLog(log, logger, builderName);
            }
        }

        private void WriteLog(string log, ILogger logger, string builderName)
        {
            if (string.IsNullOrWhiteSpace(log))
            {
                return;
            }

            Dictionary<string, object>? parsedLog;

            try
            {
                parsedLog = JsonCodec.Decode<Dictionary<string, object>>(log);
            }
            catch (JsonException ex)
            {
                return;
            }

            if (parsedLog is null)
            {
                return;
            }

            var mergedLog = new Dictionary<string, object>(parsedLog) { ["server.name"] = builderName };

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

            var effectiveLogLevel = DetermineLogLevel(parsedLog, _replayLevel);

            // Flatten the dictionary into structured log properties
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
    }
}

using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Json;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Logging
{
    public static class JsonMemorySinkExtensions
    {
        /// <summary>
        /// Adds a sink that writes JSON-formatted log events to an in-memory thread-safe list.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">A JsonFormatter to use for formatting events.</param>
        /// <param name="sinkInstance">The created sink instance (so the caller can retrieve the logs later).</param>
        public static LoggerConfiguration JsonMemorySink(this LoggerSinkConfiguration loggerConfiguration, JsonFormatter formatter, out JsonMemorySink sinkInstance)
        {
            sinkInstance = new JsonMemorySink(formatter);
            return loggerConfiguration.Sink(sinkInstance);
        }
    }
}

using System.Collections.Concurrent;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace UKHO.ADDS.EFS.Builder.S100.Logging
{
    internal class JsonMemorySink : ILogEventSink
    {
        private readonly ITextFormatter _formatter;
        private readonly ConcurrentQueue<string> _logLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMemorySink"/> class.
        /// </summary>
        /// <param name="formatter">The JSON formatter to use.</param>
        public JsonMemorySink(ITextFormatter formatter)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _logLines = new ConcurrentQueue<string>();
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            using var writer = new StringWriter(new StringBuilder());

            _formatter.Format(logEvent, writer);
            var formatted = writer.ToString();

            _logLines.Enqueue(formatted);
        }

        /// <summary>
        /// Returns a snapshot of the currently stored JSON log lines.
        /// </summary>
        /// <returns>A list of JSON strings.</returns>
        public IEnumerable<string> GetLogLines()
        {
            return [.._logLines];
        }
    }
}

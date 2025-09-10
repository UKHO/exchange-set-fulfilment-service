using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    [ExcludeFromCodeCoverage]
    public class LogEntry
    {
        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("Level")]
        public string Level { get; set; }

        [JsonPropertyName("MessageTemplate")]
        public string MessageTemplate { get; set; }

        [JsonPropertyName("Properties")]
        public Dictionary<string, object> LogProperties { get; set; }

        public EventId EventId { get; set; }
        public Exception Exception { get; set; }
    }
}

using System.Text.Json.Nodes;

namespace UKHO.ADDS.Configuration
{
    internal sealed class FlattenedProperty
    {
        public required JsonNode? JsonValue { get; init; }
        public string? Type { get; init; }
        public bool Required { get; init; }
        public bool Secret { get; init; }
    }
}

using System.Text.Json.Serialization;

namespace UKHO.ADDS.Configuration.Schema
{
    internal sealed class SchemaMetadata
    {
        [JsonPropertyName("type")] public string? Type { get; init; }

        [JsonPropertyName("required")] public bool Required { get; init; }

        [JsonPropertyName("secret")] public bool Secret { get; init; }
    }
}

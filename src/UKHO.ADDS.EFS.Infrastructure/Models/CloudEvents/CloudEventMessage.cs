using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Infrastructure.Models.CloudEvents
{
    /// <summary>
    /// Represents a CloudEvent 1.0 message for Exchange Set notifications
    /// </summary>
    public class CloudEventMessage
    {
        /// <summary>
        /// The version of the CloudEvents specification used
        /// </summary>
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        /// <summary>
        /// The type of event
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "uk.co.admiralty.avcsData.exchangeSetCreated.v1";

        /// <summary>
        /// The source of the event
        /// </summary>
        [JsonPropertyName("source")]
        public string Source { get; set; } = "https://exchangeset.admiralty.co.uk/avcsData";

        /// <summary>
        /// The unique identifier for this event
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// The time when the event occurred
        /// </summary>
        [JsonPropertyName("time")]
        public required DateTime Time { get; set; }

        /// <summary>
        /// The subject of the event
        /// </summary>
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "Requested AVCS Exchange Set Created";

        /// <summary>
        /// The content type of the data
        /// </summary>
        [JsonPropertyName("datacontenttype")]
        public string DataContentType { get; set; } = "application/json";

        /// <summary>
        /// The actual event data
        /// </summary>
        [JsonPropertyName("data")]
        public required object Data { get; set; }
    }
}

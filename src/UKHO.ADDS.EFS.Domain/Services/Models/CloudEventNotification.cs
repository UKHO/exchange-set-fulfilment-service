using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Domain.Services.Models
{
    /// <summary>
    /// CloudEvents 1.0 specification compliant notification model
    /// </summary>
    public class CloudEventNotification
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "uk.co.admiralty.avcsData.exchangeSetCreated.v1";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "https://exchangeset.admiralty.co.uk/avcsData";

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("time")]
        public string Time { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "Requested AVCS Exchange Set Created";

        [JsonPropertyName("datacontenttype")]
        public string DataContentType { get; set; } = "application/json";

        [JsonPropertyName("data")]
        public object Data { get; set; } = new();
    }
}

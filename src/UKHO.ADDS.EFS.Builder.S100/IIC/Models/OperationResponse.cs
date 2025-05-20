using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Builder.S100.IIC.Models
{
    public class OperationResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}

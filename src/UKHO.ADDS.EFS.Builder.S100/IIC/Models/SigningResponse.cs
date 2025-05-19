using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Builder.S100.IIC.Models
{
    public class SigningResponse
    {
        [JsonPropertyName("certificate")]
        public string Certificate { get; set; }

        [JsonPropertyName("signingkey")]
        public string SigningKey { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}

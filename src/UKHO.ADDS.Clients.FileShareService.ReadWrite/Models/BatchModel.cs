using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public class BatchModel
    {
        [JsonPropertyName("businessUnit")]
        public string BusinessUnit { get; set; }

        [JsonPropertyName("acl")]
        public Acl Acl { get; set; }

        [JsonPropertyName("attributes")]
        public IList<KeyValuePair<string, string>> Attributes { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateTime? ExpiryDate { get; set; }
    }
}

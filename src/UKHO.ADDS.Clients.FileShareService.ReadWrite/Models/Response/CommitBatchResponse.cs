using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response
{
    public class CommitBatchResponse
    {
        [JsonPropertyName("status")]
        public CommitBatchStatus Status { get; set; }
    }

    public class CommitBatchStatus
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response
{
    public class AddFileToBatchResponse
    {
        [JsonPropertyName("attributes")]
        public IList<KeyValuePair<string, string>> Attributes { get; set; }
    }
}

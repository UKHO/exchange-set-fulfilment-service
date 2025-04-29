using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    public class SearchBatchResponse
    {
        public int Count { get; set; }

        public int Total { get; set; }

        public List<BatchDetail> Entries { get; set; }

        [JsonPropertyName("_links")]
        public PagingLinks? Links { get; set; }

        [JsonIgnore]
        public int QueryCount { get; set; }
    }
}

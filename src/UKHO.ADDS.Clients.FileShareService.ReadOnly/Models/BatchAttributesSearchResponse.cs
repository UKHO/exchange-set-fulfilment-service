using System.Text;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    public class BatchAttributesSearchResponse
    {
        public BatchAttributesSearchResponse(int searchBatchCount, List<BatchAttributesSearchAttribute> batchAttributes)
        {
            SearchBatchCount = searchBatchCount;
            BatchAttributes = batchAttributes;
        }

        [JsonConstructor]
        internal BatchAttributesSearchResponse()
        {
        }

        public int? SearchBatchCount { get; set; }

        public List<BatchAttributesSearchAttribute> BatchAttributes { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"class {nameof(BatchAttributesSearchResponse)} {"{"}\n");
            sb.Append($" {nameof(SearchBatchCount)}: {SearchBatchCount}\n");
            sb.Append($" {nameof(BatchAttributes)}: {string.Join(", ", BatchAttributes)}\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}

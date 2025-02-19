using System.Runtime.Serialization;
using System.Text;

namespace UKHO.Clients.FileShare.Models
{
    [DataContract]
    public class BatchAttributesSearchResponse
    {
        public BatchAttributesSearchResponse(int searchBatchCount = default, List<BatchAttributesSearchAttribute> batchAttributes = default)
        {
            SearchBatchCount = searchBatchCount;
            BatchAttributes = batchAttributes;
        }

        [DataMember(Name = "searchBatchCount", EmitDefaultValue = false)]
        public int? SearchBatchCount { get; set; }

        [DataMember(Name = "batchAttributes", EmitDefaultValue = false)]
        public List<BatchAttributesSearchAttribute> BatchAttributes { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"class {nameof(BatchAttributesSearchResponse)} {"{"}\n");
            sb.Append($" {nameof(SearchBatchCount)}: {(SearchBatchCount)}\n");
            sb.Append($" {nameof(BatchAttributes)}: {string.Join(", ", BatchAttributes)}\n");
            sb.Append("}\n");
            return sb.ToString();
        }     
    }
}

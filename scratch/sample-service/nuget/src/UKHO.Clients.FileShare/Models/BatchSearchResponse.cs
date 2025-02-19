using System.Runtime.Serialization;
using System.Text;
using UKHO.Infrastructure.Serialization.Json;

namespace UKHO.Clients.FileShare.Models
{
    [DataContract]
    public class BatchSearchResponse : IEquatable<BatchSearchResponse>
    {
        public BatchSearchResponse(int? count = default, int? total = default, List<BatchDetails> entries = default,
            Links links = default)
        {
            Count = count;
            Total = total;
            Entries = entries;
            Links = links;
        }

        [DataMember(Name = "count", EmitDefaultValue = false)]
        public int? Count { get; set; }

        [DataMember(Name = "total", EmitDefaultValue = false)]
        public int? Total { get; set; }

        [DataMember(Name = "entries", EmitDefaultValue = false)]
        public List<BatchDetails> Entries { get; set; }

        [DataMember(Name = "_links", EmitDefaultValue = false)]
        public Links Links { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BatchSearchResponse {\n");
            sb.Append("  Count: ").Append(Count).Append("\n");
            sb.Append("  Total: ").Append(Total).Append("\n");
            sb.Append("  Entries: ").Append(Entries).Append("\n");
            sb.Append("  Links: ").Append(Links).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson()
        {
            return JsonConverter.Serialize(this);
        }

        public override bool Equals(object input)
        {
            return Equals(input as BatchSearchResponse);
        }

        public bool Equals(BatchSearchResponse input)
        {
            if (input == null)
                return false;

            return
                (
                    Count == input.Count ||
                    Count != null &&
                    Count.Equals(input.Count)
                ) &&
                (
                    Total == input.Total ||
                    Total != null &&
                    Total.Equals(input.Total)
                ) &&
                (
                    Entries == input.Entries ||
                    Entries != null &&
                    input.Entries != null &&
                    Entries.SequenceEqual(input.Entries)
                ) &&
                (
                    Links == input.Links ||
                    Links != null &&
                    Links.Equals(input.Links)
                );
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Count != null)
                    hashCode = hashCode * 59 + Count.GetHashCode();
                if (Total != null)
                    hashCode = hashCode * 59 + Total.GetHashCode();
                if (Entries != null)
                    hashCode = hashCode * 59 + Entries.GetHashCode();
                if (Links != null)
                    hashCode = hashCode * 59 + Links.GetHashCode();
                return hashCode;
            }
        }
    }
}

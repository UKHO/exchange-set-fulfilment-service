using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class BatchStatusResponse
    {
        public enum StatusEnum
        {
            Incomplete = 1,
            CommitInProgress = 2,
            Committed = 3,
            Rolledback = 4,
            Failed = 5
        }


        public BatchStatusResponse(string batchId, StatusEnum? status)
        {
            BatchId = batchId;
            Status = status;
        }

        [JsonConstructor]
        internal BatchStatusResponse()
        {
        }

        [JsonPropertyName("batchId")] public string BatchId { get; set; }

        [JsonPropertyName("status")] public StatusEnum? Status { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"class  {GetType().Name}{{\n");
            sb.Append("  BatchId: ").Append(BatchId).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as BatchStatusResponse);

        public bool Equals(BatchStatusResponse input)
        {
            if (input == null)
            {
                return false;
            }

            return
                (
                    BatchId == input.BatchId ||
                    (BatchId != null &&
                     BatchId.Equals(input.BatchId))
                ) &&
                (
                    Status == input.Status ||
                    (Status != null &&
                     Status.Equals(input.Status))
                );
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (BatchId != null)
                {
                    hashCode = hashCode * 59 + BatchId.GetHashCode();
                }

                if (Status != null)
                {
                    hashCode = hashCode * 59 + Status.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

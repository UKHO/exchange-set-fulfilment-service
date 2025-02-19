using System.Runtime.Serialization;
using System.Text;
using UKHO.Infrastructure.Serialization.Json;

namespace UKHO.Clients.FileShare.Models
{
    [DataContract]
    public class BatchStatusResponse
    {
        public enum StatusEnum
        {
            [EnumMember(Value = "Incomplete")] Incomplete = 1,

            [EnumMember(Value = "CommitInProgress")]
            CommitInProgress = 2,
            [EnumMember(Value = "Committed")] Committed = 3,
            [EnumMember(Value = "Rolledback")] Rolledback = 4,
            [EnumMember(Value = "Failed")] Failed = 5
        }


        public BatchStatusResponse(string batchId = default, StatusEnum? status = default)
        {
            BatchId = batchId;
            Status = status;
        }

        [DataMember(Name = "batchId", EmitDefaultValue = false)]
        public string BatchId { get; set; }

        [DataMember(Name = "status", EmitDefaultValue = false)]
        public StatusEnum? Status { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"class  {GetType().Name}{{\n");
            sb.Append("  BatchId: ").Append(BatchId).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson()
        {
            return JsonConverter.Serialize(this);
        }

        public override bool Equals(object input)
        {
            return Equals(input as BatchStatusResponse);
        }

        public bool Equals(BatchStatusResponse input)
        {
            if (input == null)
                return false;

            return
                (
                    BatchId == input.BatchId ||
                    BatchId != null &&
                    BatchId.Equals(input.BatchId)
                ) &&
                (
                    Status == input.Status ||
                    Status != null &&
                    Status.Equals(input.Status)
                );
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (BatchId != null)
                    hashCode = hashCode * 59 + BatchId.GetHashCode();
                if (Status != null)
                    hashCode = hashCode * 59 + Status.GetHashCode();
                return hashCode;
            }
        }
    }
}

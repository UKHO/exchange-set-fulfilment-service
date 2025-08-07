using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class BatchDetails : IEquatable<BatchDetails>
    {
        public enum StatusEnum
        {
            Incomplete = 1,
            Committing = 2,
            Committed = 3,
            Rolledback = 4,
            Failed = 5
        }


        public BatchDetails(string batchId = default, StatusEnum? status = default,
            List<BatchDetailsAttributes> attributes = default, string businessUnit = default,
            DateTime? batchPublishedDate = default, DateTime? expiryDate = default,
            List<BatchDetailsFiles> files = default, long? allFilesZipSize = default)
        {
            BatchId = batchId;
            Status = status;
            Attributes = attributes;
            BusinessUnit = businessUnit;
            BatchPublishedDate = batchPublishedDate;
            ExpiryDate = expiryDate;
            Files = files;
            AllFilesZipSize = allFilesZipSize;
        }

        [JsonConstructor]
        internal BatchDetails()
        {
        }

        [JsonPropertyName("batchId")] public string BatchId { get; set; }

        [JsonPropertyName("status")] public StatusEnum? Status { get; set; }

        [JsonPropertyName("attributes")] public List<BatchDetailsAttributes> Attributes { get; set; }

        [JsonPropertyName("businessUnit")] public string BusinessUnit { get; set; }

        [JsonPropertyName("batchPublishedDate")] public DateTime? BatchPublishedDate { get; set; }

        [JsonPropertyName("expiryDate")] public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("files")] public List<BatchDetailsFiles> Files { get; set; }

        [JsonPropertyName("allFilesZipSize")] public long? AllFilesZipSize { get; set; }

        public bool Equals(BatchDetails input)
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
                ) &&
                (
                    Attributes == input.Attributes ||
                    (Attributes != null &&
                     input.Attributes != null &&
                     Attributes.SequenceEqual(input.Attributes))
                ) &&
                (
                    BusinessUnit == input.BusinessUnit ||
                    (BusinessUnit != null &&
                     BusinessUnit.Equals(input.BusinessUnit))
                ) &&
                (
                    BatchPublishedDate == input.BatchPublishedDate ||
                    (BatchPublishedDate != null &&
                     BatchPublishedDate.Equals(input.BatchPublishedDate))
                ) &&
                (
                    ExpiryDate == input.ExpiryDate ||
                    (ExpiryDate != null &&
                     ExpiryDate.Equals(input.ExpiryDate))
                ) &&
                (
                    Files == input.Files ||
                    (Files != null &&
                     input.Files != null &&
                     Files.SequenceEqual(input.Files))
                ) &&
                (
                    AllFilesZipSize == input.AllFilesZipSize ||
                    (AllFilesZipSize != null &&
                     AllFilesZipSize.Equals(input.AllFilesZipSize))
                );
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BatchDetails {\n");
            sb.Append("  BatchId: ").Append(BatchId).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  Attributes: ").Append(Attributes).Append("\n");
            sb.Append("  BusinessUnit: ").Append(BusinessUnit).Append("\n");
            sb.Append("  BatchPublishedDate: ").Append(BatchPublishedDate).Append("\n");
            sb.Append("  ExpiryDate: ").Append(ExpiryDate).Append("\n");
            sb.Append("  Files: ").Append(Files).Append("\n");
            sb.Append("  AllFilesZipSize: ").Append(AllFilesZipSize).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as BatchDetails);

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

                if (BusinessUnit != null)
                {
                    hashCode = hashCode * 59 + BusinessUnit.GetHashCode();
                }

                if (BatchPublishedDate != null)
                {
                    hashCode = hashCode * 59 + BatchPublishedDate.GetHashCode();
                }

                if (ExpiryDate != null)
                {
                    hashCode = hashCode * 59 + ExpiryDate.GetHashCode();
                }

                if (Files != null)
                {
                    hashCode = hashCode * 59 + Files.GetHashCode();
                }

                if (AllFilesZipSize != null)
                {
                    hashCode = hashCode * 59 + AllFilesZipSize.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

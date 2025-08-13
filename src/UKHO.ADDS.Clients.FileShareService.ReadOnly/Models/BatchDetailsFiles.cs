using System.Runtime.Serialization;
using System.Text;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class BatchDetailsFiles : IEquatable<BatchDetailsFiles>
    {
        public BatchDetailsFiles(string filename = default, long? fileSize = default, string mimeType = default,
            string hash = default, List<BatchDetailsAttributes> attributes = default, BatchDetailsLinks links = default)
        {
            Filename = filename;
            FileSize = fileSize;
            MimeType = mimeType;
            Hash = hash;
            Attributes = attributes;
            Links = links;
        }

        [DataMember(Name = "filename", EmitDefaultValue = false)] public string Filename { get; set; }

        [DataMember(Name = "fileSize", EmitDefaultValue = false)] public long? FileSize { get; set; }

        [DataMember(Name = "mimeType", EmitDefaultValue = false)] public string MimeType { get; set; }

        [DataMember(Name = "hash", EmitDefaultValue = false)] public string Hash { get; set; }

        [DataMember(Name = "attributes", EmitDefaultValue = false)] public List<BatchDetailsAttributes> Attributes { get; set; }

        [DataMember(Name = "links", EmitDefaultValue = false)] public BatchDetailsLinks Links { get; set; }

        public bool Equals(BatchDetailsFiles input)
        {
            if (input == null)
            {
                return false;
            }

            return
                (
                    Filename == input.Filename ||
                    (Filename != null &&
                     Filename.Equals(input.Filename))
                ) &&
                (
                    FileSize == input.FileSize ||
                    (FileSize != null &&
                     FileSize.Equals(input.FileSize))
                ) &&
                (
                    MimeType == input.MimeType ||
                    (MimeType != null &&
                     MimeType.Equals(input.MimeType))
                ) &&
                (
                    Hash == input.Hash ||
                    (Hash != null &&
                     Hash.Equals(input.Hash))
                ) &&
                (
                    Attributes == input.Attributes ||
                    (Attributes != null &&
                     input.Attributes != null &&
                     Attributes.SequenceEqual(input.Attributes))
                ) &&
                (
                    Links == input.Links ||
                    (Links != null &&
                     Links.Equals(input.Links))
                );
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BatchDetailsFiles {\n");
            sb.Append("  Filename: ").Append(Filename).Append("\n");
            sb.Append("  FileSize: ").Append(FileSize).Append("\n");
            sb.Append("  MimeType: ").Append(MimeType).Append("\n");
            sb.Append("  Hash: ").Append(Hash).Append("\n");
            sb.Append("  Attributes: ").Append(Attributes).Append("\n");
            sb.Append("  Links: ").Append(Links).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as BatchDetailsFiles);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Filename != null)
                {
                    hashCode = hashCode * 59 + Filename.GetHashCode();
                }

                if (FileSize != null)
                {
                    hashCode = hashCode * 59 + FileSize.GetHashCode();
                }

                if (MimeType != null)
                {
                    hashCode = hashCode * 59 + MimeType.GetHashCode();
                }

                if (Hash != null)
                {
                    hashCode = hashCode * 59 + Hash.GetHashCode();
                }

                if (Links != null)
                {
                    hashCode = hashCode * 59 + Links.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

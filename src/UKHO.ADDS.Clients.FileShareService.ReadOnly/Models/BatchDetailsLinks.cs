using System.Runtime.Serialization;
using System.Text;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class BatchDetailsLinks : IEquatable<BatchDetailsLinks>
    {
        public BatchDetailsLinks(Link get = default) => Get = get;


        [DataMember(Name = "get", EmitDefaultValue = false)] public Link Get { get; set; }

        public bool Equals(BatchDetailsLinks input)
        {
            if (input == null)
            {
                return false;
            }

            return
                Get == input.Get ||
                (Get != null &&
                 Get.Equals(input.Get));
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BatchDetailsLinks {\n");
            sb.Append("  Get: ").Append(Get).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as BatchDetailsLinks);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Get != null)
                {
                    hashCode = hashCode * 59 + Get.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

using System.Runtime.Serialization;
using System.Text;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class BatchDetailsAttributes : IEquatable<BatchDetailsAttributes>
    {
        public BatchDetailsAttributes(string key = default, string value = default)
        {
            Key = key;
            Value = value;
        }

        [DataMember(Name = "key", EmitDefaultValue = false)] public string Key { get; set; }

        [DataMember(Name = "value", EmitDefaultValue = false)] public string Value { get; set; }

        public bool Equals(BatchDetailsAttributes input)
        {
            if (input == null)
            {
                return false;
            }

            return
                (
                    Key == input.Key ||
                    (Key != null &&
                     Key.Equals(input.Key))
                ) &&
                (
                    Value == input.Value ||
                    (Value != null &&
                     Value.Equals(input.Value))
                );
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BatchDetailsAttributes {\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as BatchDetailsAttributes);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Key != null)
                {
                    hashCode = hashCode * 59 + Key.GetHashCode();
                }

                if (Value != null)
                {
                    hashCode = hashCode * 59 + Value.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

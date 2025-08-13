using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    [DataContract]
    public class Link : IEquatable<Link>
    {
        public Link(string href) => Href = href;

        [JsonConstructor]
        internal Link()
        {
        }

        [JsonPropertyName("href")] public string Href { get; set; }

        public bool Equals(Link input)
        {
            if (input == null)
            {
                return false;
            }

            return
                Href == input.Href ||
                (Href != null &&
                 Href.Equals(input.Href));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Link {\n");
            sb.Append("  Href: ").Append(Href).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        ///     Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson() => JsonCodec.Encode(this, JsonCodec.DefaultOptionsNoFormat);

        public override bool Equals(object input) => Equals(input as Link);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Href != null)
                {
                    hashCode = hashCode * 59 + Href.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}

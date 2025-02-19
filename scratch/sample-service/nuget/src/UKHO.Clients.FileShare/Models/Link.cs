using System.Runtime.Serialization;
using System.Text;
using UKHO.Infrastructure.Serialization.Json;

namespace UKHO.Clients.FileShare.Models
{
    [DataContract]
    public class Link : IEquatable<Link>
    {
        public Link(string href = default)
        {
            Href = href;
        }

        [DataMember(Name = "href", EmitDefaultValue = false)]
        public string Href { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Link {\n");
            sb.Append("  Href: ").Append(Href).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConverter.Serialize(this);
        }

        public override bool Equals(object input)
        {
            return Equals(input as Link);
        }

        public bool Equals(Link input)
        {
            if (input == null)
                return false;

            return
                Href == input.Href ||
                Href != null &&
                Href.Equals(input.Href);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Href != null)
                    hashCode = hashCode * 59 + Href.GetHashCode();
                return hashCode;
            }
        }
    }
}

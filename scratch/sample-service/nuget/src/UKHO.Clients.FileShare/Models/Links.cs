using System.Runtime.Serialization;
using System.Text;
using UKHO.Infrastructure.Serialization.Json;

namespace UKHO.Clients.FileShare.Models
{
    [DataContract]
    public class Links : IEquatable<Links>
    {
        public Links(Link self = default, Link first = default, Link previous = default, Link next = default,
            Link last = default)
        {
            Self = self;
            First = first;
            Previous = previous;
            Next = next;
            Last = last;
        }

        [DataMember(Name = "self", EmitDefaultValue = false)]
        public Link Self { get; set; }

        [DataMember(Name = "first", EmitDefaultValue = false)]
        public Link First { get; set; }

        [DataMember(Name = "previous", EmitDefaultValue = false)]
        public Link Previous { get; set; }

        [DataMember(Name = "next", EmitDefaultValue = false)]
        public Link Next { get; set; }

        [DataMember(Name = "last", EmitDefaultValue = false)]
        public Link Last { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Links {\n");
            sb.Append("  Self: ").Append(Self).Append("\n");
            sb.Append("  First: ").Append(First).Append("\n");
            sb.Append("  Previous: ").Append(Previous).Append("\n");
            sb.Append("  Next: ").Append(Next).Append("\n");
            sb.Append("  Last: ").Append(Last).Append("\n");
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
            return Equals(input as Links);
        }

        public bool Equals(Links input)
        {
            if (input == null)
                return false;

            return
                (
                    Self == input.Self ||
                    Self != null &&
                    Self.Equals(input.Self)
                ) &&
                (
                    First == input.First ||
                    First != null &&
                    First.Equals(input.First)
                ) &&
                (
                    Previous == input.Previous ||
                    Previous != null &&
                    Previous.Equals(input.Previous)
                ) &&
                (
                    Next == input.Next ||
                    Next != null &&
                    Next.Equals(input.Next)
                ) &&
                (
                    Last == input.Last ||
                    Last != null &&
                    Last.Equals(input.Last)
                );
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                if (Self != null)
                    hashCode = hashCode * 59 + Self.GetHashCode();
                if (First != null)
                    hashCode = hashCode * 59 + First.GetHashCode();
                if (Previous != null)
                    hashCode = hashCode * 59 + Previous.GetHashCode();
                if (Next != null)
                    hashCode = hashCode * 59 + Next.GetHashCode();
                if (Last != null)
                    hashCode = hashCode * 59 + Last.GetHashCode();
                return hashCode;
            }
        }
    }
}

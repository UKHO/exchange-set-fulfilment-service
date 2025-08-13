using System.Text;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Models
{
    public class BatchAttributesSearchAttribute
    {
        public BatchAttributesSearchAttribute(string key, List<string> values)
        {
            Key = key;
            Values = values;
        }

        [JsonConstructor]
        internal BatchAttributesSearchAttribute()
        {
        }

        public string Key { get; set; }

        public List<string> Values { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"class {nameof(BatchAttributesSearchAttribute)} {"{"}\n");
            sb.Append($" {nameof(Key)}: {Key}\n");
            sb.Append($" {nameof(Values)}: {string.Join(", ", Values)}\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}

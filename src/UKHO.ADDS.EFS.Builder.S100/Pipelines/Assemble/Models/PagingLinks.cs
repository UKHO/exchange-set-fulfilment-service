using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    public class PagingLinks
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Link Self { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Link First { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Link Previous { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Link Next { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Link Last { get; set; }
    }
}

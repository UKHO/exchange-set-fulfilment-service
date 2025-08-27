using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Products
{
    [ExcludeFromCodeCoverage]
    public class S100ProductStatus
    {
        [JsonPropertyName("statusName")]
        public string? StatusName { get; set; }

        [JsonPropertyName("statusDate")]
        public DateTime StatusDate { get; set; }
    }
}

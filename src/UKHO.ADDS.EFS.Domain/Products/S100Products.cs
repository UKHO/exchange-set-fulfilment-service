using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Products
{
    [ExcludeFromCodeCoverage]
    public class S100Products
    {
        [JsonPropertyName("productName")]
        public ProductName ProductName { get; set; }

        [JsonPropertyName("latestEditionNumber")]
        public EditionNumber LatestEditionNumber { get; set; }

        [JsonPropertyName("latestUpdateNumber")]
        public UpdateNumber LatestUpdateNumber { get; set; }

        [JsonPropertyName("status")]
        public S100ProductStatus? Status { get; set; }
    }
}

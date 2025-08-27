using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Products
{
    [ExcludeFromCodeCoverage]
    public class ProductCancellation
    {
        [JsonPropertyName("editionNumber")]
        public EditionNumber EditionNumber { get; set; }

        [JsonPropertyName("updateNumber")]
        public UpdateNumber UpdateNumber { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100ProductCancellation
    {
        [JsonPropertyName("editionNumber")]
        public int EditionNumber { get; set; }

        [JsonPropertyName("updateNumber")]
        public int UpdateNumber { get; set; }
    }
}

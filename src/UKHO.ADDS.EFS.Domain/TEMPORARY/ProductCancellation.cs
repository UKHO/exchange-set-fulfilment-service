using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
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

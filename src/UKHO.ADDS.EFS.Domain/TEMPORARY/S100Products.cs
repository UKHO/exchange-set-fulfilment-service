using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100Products
    {
        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("latestEditionNumber")]
        public int? LatestEditionNumber { get; set; }

        [JsonPropertyName("latestUpdateNumber")]
        public int? LatestUpdateNumber { get; set; }

        [JsonPropertyName("status")]
        public S100ProductStatus? Status { get; set; }
    }
}

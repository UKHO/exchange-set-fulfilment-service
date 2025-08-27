using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
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

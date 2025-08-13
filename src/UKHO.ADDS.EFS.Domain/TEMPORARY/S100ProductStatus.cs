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
    public class S100ProductStatus
    {
        [JsonPropertyName("statusName")]
        public string? StatusName { get; set; }

        [JsonPropertyName("statusDate")]
        public DateTime StatusDate { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100ProductNamesResponse
    {
        [JsonPropertyName("productCounts")]
        public ProductCounts ProductCounts { get; set; } = new ProductCounts();
        
        [JsonPropertyName("products")]
        public List<S100ProductNames> Products { get; set; } = new List<S100ProductNames>();
        
        [JsonIgnore]
        public HttpStatusCode ResponseCode { get; set; }
    }
}

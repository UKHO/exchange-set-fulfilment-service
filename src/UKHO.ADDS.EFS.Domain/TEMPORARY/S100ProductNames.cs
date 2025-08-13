using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100ProductNames
    {
        [JsonPropertyName("editionNumber")]
        public int EditionNumber { get; set; }
        
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }
        
        [JsonPropertyName("updateNumbers")]
        public List<int> UpdateNumbers { get; set; } = new List<int>();
        
        [JsonPropertyName("dates")]
        public List<S100ProductDate> Dates { get; set; } = new List<S100ProductDate>();
        
        [JsonPropertyName("fileSize")]
        public int FileSize { get; set; }
        
        [JsonPropertyName("cancellation")]
        public S100ProductCancellation Cancellation { get; set; }
    }
}

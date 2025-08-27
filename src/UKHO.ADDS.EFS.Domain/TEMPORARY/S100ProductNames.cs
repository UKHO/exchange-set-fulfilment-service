using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100ProductNames
    {
        [JsonPropertyName("editionNumber")]
        public EditionNumber EditionNumber { get; set; }
        
        [JsonPropertyName("productName")]
        public ProductName ProductName { get; set; }
        
        [JsonPropertyName("updateNumbers")]
        public List<int> UpdateNumbers { get; set; } = new List<int>();
        
        [JsonPropertyName("dates")]
        public List<S100ProductDate> Dates { get; set; } = new List<S100ProductDate>();
        
        [JsonPropertyName("fileSize")]
        public int FileSize { get; set; }
        
        [JsonPropertyName("cancellation")]
        public ProductCancellation Cancellation { get; set; }
    }
}

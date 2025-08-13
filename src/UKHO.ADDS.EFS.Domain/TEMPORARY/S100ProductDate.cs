using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    [ExcludeFromCodeCoverage]
    public class S100ProductDate
    {
        [JsonPropertyName("issueDate")]
        public DateTime IssueDate { get; set; }
        
        [JsonPropertyName("updateApplicationDate")]
        public DateTime? UpdateApplicationDate { get; set; }
        
        [JsonPropertyName("updateNumber")]
        public int UpdateNumber { get; set; }
    }
}

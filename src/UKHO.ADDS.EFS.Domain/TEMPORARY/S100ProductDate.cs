using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.VOS;

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
        public UpdateNumber UpdateNumber { get; set; }
    }
}

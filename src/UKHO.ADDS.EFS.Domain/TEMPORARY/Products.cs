using System.Text.Json.Serialization;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class Products
    {
        public ProductName ProductName { get; set; }
        public EditionNumber EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public List<Dates> Dates { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
        [JsonIgnore]
        public bool IgnoreCache { get; set; }
        public List<Bundle> Bundle { get; set; }
    }
}

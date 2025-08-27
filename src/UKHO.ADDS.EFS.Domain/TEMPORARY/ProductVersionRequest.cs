using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class ProductVersionRequest
    {
        public ProductName ProductName { get; set; }
        public EditionNumber EditionNumber { get; set; }
        public UpdateNumber UpdateNumber { get; set; }
    }
}

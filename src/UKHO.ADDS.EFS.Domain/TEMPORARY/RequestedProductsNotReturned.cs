using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class RequestedProductsNotReturned
    {
        public ProductName ProductName { get; set; }
        public string Reason { get; set; }
    }
}

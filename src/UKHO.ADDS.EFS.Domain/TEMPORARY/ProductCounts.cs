using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class ProductCounts
    {
        public ProductCount RequestedProductCount { get; set; }
        public ProductCount ReturnedProductCount { get; set; }
        public ProductCount RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }
    }
}

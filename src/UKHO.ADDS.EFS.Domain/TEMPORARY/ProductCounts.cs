namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class ProductCounts
    {
        public int? RequestedProductCount { get; set; }
        public int? ReturnedProductCount { get; set; }
        public int? RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }
    }
}

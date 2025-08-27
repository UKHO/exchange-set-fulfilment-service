namespace UKHO.ADDS.EFS.Products
{
    public class ProductCounts
    {
        public ProductCount RequestedProductCount { get; set; }
        public ProductCount ReturnedProductCount { get; set; }
        public ProductCount RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<RequestedProductsNotReturned> RequestedProductsNotReturned { get; set; }
    }
}

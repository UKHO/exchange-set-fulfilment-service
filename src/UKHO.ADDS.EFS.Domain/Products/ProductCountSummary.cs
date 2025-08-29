namespace UKHO.ADDS.EFS.Domain.Products
{
    public class ProductCountSummary
    {
        public ProductCount RequestedProductCount { get; set; }
        public ProductCount ReturnedProductCount { get; set; }
        public ProductCount RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<MissingProduct> MissingProducts { get; set; }
    }
}

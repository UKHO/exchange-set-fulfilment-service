namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Models
{
    public class ProductNamesResponse
    {
        public ProductCounts ProductCounts { get; set; }
        public List<Product> Products { get; set; }
    }

    public class ProductCounts
    {
        public int RequestedProductCount { get; set; }
        public int ReturnedProductCount { get; set; }
        public int RequestedProductsAlreadyUpToDateCount { get; set; }
        public List<string>? RequestedProductsNotReturned { get; set; }
    }

    public class Product
    {
        public int EditionNumber { get; set; }
        public string? ProductName { get; set; }
        public List<int>? UpdateNumbers { get; set; }
        public List<ProductDate> Dates { get; set; }
        public int FileSize { get; set; }
    }

    public class ProductDate
    {
        public DateTime IssueDate { get; set; }
        public DateTime? UpdateApplicationDate { get; set; }
        public int UpdateNumber { get; set; }
    }
}

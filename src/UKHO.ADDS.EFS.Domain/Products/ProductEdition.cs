namespace UKHO.ADDS.EFS.Domain.Products
{
    public class ProductEdition
    {
        public EditionNumber EditionNumber { get; set; }
        
        public ProductName ProductName { get; set; }
        
        public List<int> UpdateNumbers { get; set; } = new List<int>();
        
        public List<ProductDate> Dates { get; set; } = new List<ProductDate>();
        
        public int FileSize { get; set; }
        
        public ProductCancellation Cancellation { get; set; }
    }
}

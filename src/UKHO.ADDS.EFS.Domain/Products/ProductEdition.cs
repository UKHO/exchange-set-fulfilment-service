namespace UKHO.ADDS.EFS.Domain.Products
{
    public class ProductEdition
    {
        public EditionNumber EditionNumber { get; set; }
        
        public ProductName ProductName { get; set; }
        
        public UpdateNumberList UpdateNumbers { get; set; } = new UpdateNumberList();
        
        public ProductDateList Dates { get; set; } = new ProductDateList();
        
        public int FileSize { get; set; }
        
        public ProductCancellation Cancellation { get; set; }
    }
}

namespace UKHO.ADDS.EFS.Domain.Products
{
    public class Product
    {
        public ProductName ProductName { get; set; }

        public EditionNumber LatestEditionNumber { get; set; }

        public UpdateNumber LatestUpdateNumber { get; set; }

        public ProductStatus? Status { get; set; }
    }
}

namespace UKHO.ADDS.EFS.Products
{
    public class ProductVersion
    {
        public ProductName ProductName { get; set; }

        public EditionNumber LatestEditionNumber { get; set; }

        public UpdateNumber LatestUpdateNumber { get; set; }

        public ProductStatus? Status { get; set; }
    }
}

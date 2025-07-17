namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss.Models
{
    public class FSSSearchFilterDetails
    {
        public List<Product> Products { get; set; } = [];
        public string? BusinessUnit { get; set; }
        public string ProductType { get; set; } = string.Empty;
    }
}

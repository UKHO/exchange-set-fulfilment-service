namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.scs.Models
{
    public class ProductVersionRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public int? EditionNumber { get; set; }
        public int UpdateNumber { get; set; }
    }
}

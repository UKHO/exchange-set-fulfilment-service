namespace UKHO.ADDS.Mocks.SampleService.Override.Mocks.fss.Models
{
    public class Product
    {
        public string ProductName { get; set; } = string.Empty;
        public int EditionNumber { get; set; }
        public List<int> UpdateNumbers { get; set; } = [];
    }
}

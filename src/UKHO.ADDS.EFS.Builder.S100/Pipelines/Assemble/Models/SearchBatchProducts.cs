namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    public class SearchBatchProducts
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
    }
}

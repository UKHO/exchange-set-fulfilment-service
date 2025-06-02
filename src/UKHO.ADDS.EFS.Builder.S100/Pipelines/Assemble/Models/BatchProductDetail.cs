namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models
{
    internal class BatchProductDetail
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public IEnumerable<int?> UpdateNumbers { get; set; }
    }
}

using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class BatchSearchProductsLogView
    {
        public List<SearchBatchProducts> BatchProducts { get; set; }
        public string BusinessUnit { get; set; }
        public string ProductType { get; set; }
        public string CorrelationId { get; set; }
        public SearchQuery Query { get; set; }
        public string Error { get; set; }

    }
}

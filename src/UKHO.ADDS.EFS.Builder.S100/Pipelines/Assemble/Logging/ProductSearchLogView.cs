using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class ProductSearchLogView
    {
        public string CorrelationId { get; set; }
        public IEnumerable<BatchProductDetail> GroupedProducts { get; set; }
        public IEnumerable<BatchDetails> BatchDetails { get; set; }
        public int ProductCount { get; set; }
        public int BatchCount { get; set; }
    }
}

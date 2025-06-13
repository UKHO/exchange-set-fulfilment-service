using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class BatchProductSearchLog
    {
        public IEnumerable<BatchProductDetail> BatchProducts { get; set; }
        public string BusinessUnit { get; set; }
        public string ProductType { get; set; }
        public string CorrelationId { get; set; }
        public SearchQuery Query { get; set; }
        public IError Error { get; set; }

    }
}

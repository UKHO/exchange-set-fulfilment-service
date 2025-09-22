using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class BatchProductSearchLog
    {
        public string BusinessUnit { get; set; }
        public string ProductCode { get; set; }
        public SearchQuery Query { get; set; }
        public IError Error { get; set; }

    }
}

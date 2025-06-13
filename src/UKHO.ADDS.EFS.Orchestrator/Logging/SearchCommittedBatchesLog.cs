using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Logging
{
    internal class SearchCommittedBatchesLog
    {
        public string BusinessUnit { get; set; }
        public string ProductType { get; set; }
        public string CorrelationId { get; set; }
        public SearchQuery Query { get; set; }
        public IError Error { get; set; }
    }
}

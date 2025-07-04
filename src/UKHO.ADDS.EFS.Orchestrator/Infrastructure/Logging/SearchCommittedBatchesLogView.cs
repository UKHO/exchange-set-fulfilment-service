using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal class SearchCommittedBatchesLogView
    {
        public string BusinessUnit { get; set; }
        public string BatchId { get; set; }
        public string ProductType { get; set; }
        public string CorrelationId { get; set; }
        public SearchQueryLogView Query { get; set; }
        public IError Error { get; set; }
    }
}

using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    internal class SearchCommittedBatchesLogView
    {
        public string BusinessUnit { get; set; }
        public BatchId BatchId { get; set; }
        public string ProductCode { get; set; }
        public CorrelationId CorrelationId { get; set; }
        public SearchQueryLogView Query { get; set; }
        public IError Error { get; set; }
    }
}

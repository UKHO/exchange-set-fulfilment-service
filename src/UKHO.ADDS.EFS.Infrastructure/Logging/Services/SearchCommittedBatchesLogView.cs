using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    internal class SearchCommittedBatchesLogView
    {
        public required string BusinessUnit { get; init; }
        public required BatchId BatchId { get; init; }
        public required string ProductCode { get; init; }
        public required CorrelationId CorrelationId { get; init; }
        [LogProperties]
        public required SearchQueryLogView Query { get; init; }
        [LogProperties]
        public required IError Error { get; init; }
    }
}

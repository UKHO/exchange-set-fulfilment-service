using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    internal class FileShareServiceLogView
    {
        public required BatchId BatchId { get; init; }
        public required JobId JobId { get; init; }
        public required string EndPoint { get; init; }
        public required CorrelationId CorrelationId { get; init; }
        public IError Error { get; set; }
    }
}

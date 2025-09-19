using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Infrastructure.Logging.Services
{
    /// <summary>
    /// Log view model for callback notification operations
    /// </summary>
    internal class CallbackNotificationLogView
    {
        public required JobId JobId { get; init; }
        public required CallbackUri CallbackUri { get; init; }
        public required CorrelationId CorrelationId { get; init; }
        public int? StatusCode { get; set; }
        public string? ResponseContent { get; set; }
        public string? ErrorContent { get; set; }
    }
}

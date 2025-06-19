using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Logging
{
    internal class FileShareServiceLogView
    {
        public string BatchId { get; set; }
        public string JobId { get; set; }
        public string EndPoint { get; set; }
        public string CorrelationId { get; set; }
        public IError Error { get; set; }
    }
}

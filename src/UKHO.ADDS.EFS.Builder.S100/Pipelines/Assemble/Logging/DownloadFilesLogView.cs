using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class DownloadFilesLogView
    {
        public string CorrelationId { get; set; }
        public IError Error { get; set; }
        public string FileName { get; set; }
        public string BatchId { get; set; }
    }
}

using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal class DownloadFilesLogView
    {
        public SearchBatchProducts Product { get; set; }
        public string CorrelationId { get; set; }
        public string Error { get; set; }
        public string FileName { get; set; }
        public string BatchId { get; set; }
    }
}

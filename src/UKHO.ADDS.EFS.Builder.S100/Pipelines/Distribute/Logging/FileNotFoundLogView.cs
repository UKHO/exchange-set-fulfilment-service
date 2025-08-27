using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    public class FileNotFoundLogView
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public BatchId BatchId { get; set; }
        public CorrelationId CorrelationId { get; set; }
    }
}

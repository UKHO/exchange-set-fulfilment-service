namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    public class FileNotFoundLogView
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string BatchId { get; set; }
        public string CorrelationId { get; set; }
    }
}

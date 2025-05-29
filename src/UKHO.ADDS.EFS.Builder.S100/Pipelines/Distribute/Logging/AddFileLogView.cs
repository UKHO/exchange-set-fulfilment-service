using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    public class AddFileLogView
    {
        public string FileName { get; set; }
        public string BatchId { get; set; }
        public string CorrelationId { get; set; }
        public IError Error { get; set; }
    }
}

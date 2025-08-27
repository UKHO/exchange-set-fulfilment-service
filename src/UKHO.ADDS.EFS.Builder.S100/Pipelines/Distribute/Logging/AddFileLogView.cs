using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    public class AddFileLogView
    {
        public string FileName { get; set; }
        public BatchId BatchId { get; set; }
        public IError Error { get; set; }
    }
}

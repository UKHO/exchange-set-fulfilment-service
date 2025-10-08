using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Files
{
    public class Batch
    {
        public required BatchId BatchId { get; init; }
        public DateTime BatchExpiryDateTime { get; set; } = DateTime.MinValue;  
    }
}

using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Jobs
{
    public class ExchangeSetJobType
    {
        public required string JobId { get; init; }

        public ExchangeSetDataStandard DataStandard { get; init; }

        public required DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}

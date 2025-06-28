using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Jobs
{
    public class ExchangeSetTimestamp
    {
        public ExchangeSetDataStandard DataStandard { get; set; } = ExchangeSetDataStandard.S100;

        public DateTime? Timestamp { get; set; }
    }
}

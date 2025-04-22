namespace UKHO.ADDS.EFS.Messages
{
    public class ExchangeSetRequestMessage
    {
        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }

        public string CorrelationId { get; set; }

    }
}

namespace UKHO.ADDS.EFS.Messages
{
    public class ExchangeSetRequestMessage
    {
        public required string Id { get; set; }

        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }
    }
}

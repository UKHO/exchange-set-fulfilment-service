namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="ExchangeSetRequestMessage"/> is received via the Request API (and later converted into a <see cref="ExchangeSetRequestQueueMessage"/>.
    /// </summary>
    public class ExchangeSetRequestMessage
    {
        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }
    }
}

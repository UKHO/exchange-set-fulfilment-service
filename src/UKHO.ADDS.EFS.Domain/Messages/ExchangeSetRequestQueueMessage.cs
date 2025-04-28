namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="ExchangeSetRequestQueueMessage"/> is written to the queue by the EFS service when a new exchange set request is received.
    /// </summary>
    public class ExchangeSetRequestQueueMessage
    {
        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }

        public required string CorrelationId { get; set; }

    }
}

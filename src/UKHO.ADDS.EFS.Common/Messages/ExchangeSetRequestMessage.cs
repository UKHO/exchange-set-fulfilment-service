namespace UKHO.ADDS.EFS.Common.Messages
{
    /// <summary>
    /// An example of a message to request an exchange set build (properties for demo purposes only)
    /// </summary>
    public class ExchangeSetRequestMessage
    {
        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }
    }
}

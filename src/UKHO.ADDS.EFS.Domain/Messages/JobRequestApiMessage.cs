namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestApiMessage"/> is received via the Request API (and later converted into a <see cref="JobRequestQueueMessage"/>.
    /// </summary>
    public class JobRequestApiMessage
    {
        public required int Version { get; init; } = 1;

        public ExchangeSetDataStandard DataStandard { get; set; }

        public required string Products { get; set; }
    }
}

using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestQueueMessage"/> is written to the queue by the EFS service when a new exchange set request is received.
    /// </summary>
    public class JobRequestQueueMessage
    {
        public required int Version { get; init; }

        public required DateTime Timestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string Filter { get; init; }

        public required string CorrelationId { get; init; }

    }
}

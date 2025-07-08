﻿namespace UKHO.ADDS.EFS.Messages
{
    /// <summary>
    /// <see cref="JobRequestQueueMessage"/> is written to the queue by the EFS service when a new exchange set request is received.
    /// </summary>
    public class JobRequestQueueMessage
    {
        public required int Version { get; init; }

        public required DateTime Timestamp { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public required string Products { get; init; }

        public required string ProductIdentifier { get; init; }

        public required string CorrelationId { get; init; }

    }
}

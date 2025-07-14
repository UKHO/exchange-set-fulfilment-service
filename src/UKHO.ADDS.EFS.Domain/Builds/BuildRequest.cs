using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builds
{
    public abstract class BuildRequest
    {
        /// <summary>
        /// The message version
        /// </summary>
        public required int Version { get; init; }

        /// <summary>
        /// The build request timestamp
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        /// The job ID relating to this build
        /// </summary>
        public required string JobId { get; init; }

        /// <summary>
        /// The File Share batch ID for this build
        /// </summary>
        public required string BatchId { get; init; }

        /// <summary>
        /// The data standard
        /// </summary>
        public required DataStandard DataStandard { get; init; }

        /// <summary>
        /// The Exchange Set name template
        /// </summary>
        public required string ExchangeSetNameTemplate { get; init; }
    }
}

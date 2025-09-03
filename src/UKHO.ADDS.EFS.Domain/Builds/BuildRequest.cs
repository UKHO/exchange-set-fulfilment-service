using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Messages;

namespace UKHO.ADDS.EFS.Domain.Builds
{
    public abstract class BuildRequest
    {
        /// <summary>
        /// The message version
        /// </summary>
        public MessageVersion Version { get; init; } = MessageVersion.From(1);

        /// <summary>
        /// The build request timestamp
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        /// The job ID relating to this build
        /// </summary>
        public required JobId JobId { get; init; }

        /// <summary>
        /// The File Share batch ID for this build
        /// </summary>
        public required BatchId BatchId { get; init; }

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

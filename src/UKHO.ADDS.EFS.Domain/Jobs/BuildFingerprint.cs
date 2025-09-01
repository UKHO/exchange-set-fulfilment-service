namespace UKHO.ADDS.EFS.Domain.Jobs
{
    public class BuildFingerprint
    {
        /// <summary>
        ///     The Job ID
        /// </summary>
        public required JobId JobId { get; init; }

        /// <summary>
        ///     The discriminant is a BLAKE2b-256 hash of the build discriminator string
        /// </summary>
        public required string Discriminant { get; init; }

        /// <summary>
        ///     The File Share batch ID of the Exchange Set
        /// </summary>
        public required BatchId BatchId { get; init; }

        /// <summary>
        ///     The data standard
        /// </summary>
        public required DataStandard DataStandard { get; init; }

        /// <summary>
        ///     The job timestamp
        /// </summary>
        public required DateTime Timestamp { get; init; }
    }
}

namespace UKHO.ADDS.EFS.NewEFS
{
    public class Job
    {
        /// <summary>
        ///     The job id.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        ///     The timestamp of the job creation.
        /// </summary>
        public required DateTime Timestamp { get; init; }

        /// <summary>
        ///     The job data standard, which indicates the format of the data being processed.
        /// </summary>
        public required DataStandard DataStandard { get; init; }

        /// <summary>
        /// The current state of the job.
        /// </summary>
        public required JobState JobState { get; init; }

        /// <summary>
        /// The current build state.
        /// </summary>
        public required BuildState BuildState { get; init; }

        /// <summary>
        ///     The FSS Batch ID associated with the job.
        /// </summary>
        public string? BatchId { get; set; }

        /// <summary>
        ///     Gets the correlation ID for the job.
        /// </summary>
        /// <remarks>This is always the Job ID.</remarks>
        /// <returns></returns>
        public string GetCorrelationId() => Id;
    }
}

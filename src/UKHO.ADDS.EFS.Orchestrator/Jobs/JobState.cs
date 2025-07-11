namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    public enum JobState
    {
        /// <summary>
        ///     The job has been created
        /// </summary>
        Created,

        /// <summary>
        ///     The job is up to date, meaning no new data is available for processing
        /// </summary>
        UpToDate,

        /// <summary>
        ///     The job is a duplicate request, typically indicating that it has been submitted again with the same parameters
        /// </summary>
        Duplicate,

        /// <summary>
        ///     The job has been submitted for processing
        /// </summary>
        Submitted,

        /// <summary>
        ///     The job has failed during processing
        /// </summary>
        Failed,

        /// <summary>
        ///     The job has been completed successfully without any errors
        /// </summary>
        Completed,
    }
}

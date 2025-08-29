namespace UKHO.ADDS.EFS.Domain.Builds
{
    public enum BuildState
    {
        /// <summary>
        ///     No build has been created
        /// </summary>
        None,

        /// <summary>
        ///     The build has been created but not yet scheduled
        /// </summary>
        NotScheduled,

        /// <summary>
        ///     The build has been scheduled for processing
        /// </summary>
        Scheduled,

        /// <summary>
        ///     The build failed
        /// </summary>
        Failed,

        /// <summary>
        ///     The build succeeded
        /// </summary>
        Succeeded
    }
}

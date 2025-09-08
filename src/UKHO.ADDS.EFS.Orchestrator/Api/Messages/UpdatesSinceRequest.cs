namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    ///     Request model for updates since datetime endpoint
    /// </summary>
    internal class UpdatesSinceRequest
    {
        /// <summary>
        ///     The date and time from which changes are requested
        /// </summary>
        public required DateTime SinceDateTime { get; set; }
    }
}

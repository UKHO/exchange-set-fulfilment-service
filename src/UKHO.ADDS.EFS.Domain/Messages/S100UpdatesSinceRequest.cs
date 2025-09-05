namespace UKHO.ADDS.EFS.Domain.Messages
{
    /// <summary>
    /// Request model for S100 updates since datetime endpoint
    /// </summary>
    public class S100UpdatesSinceRequest
    {
        /// <summary>
        /// The date and time from which changes are requested
        /// </summary>
        public required string SinceDateTime { get; set; }
    }
}

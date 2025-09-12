namespace UKHO.ADDS.EFS.Domain.Messages
{
    /// <summary>
    /// Request model for S100 updates since datetime endpoint
    /// </summary>
    public class S100UpdatesSinceRequest
    {
        public DateTime? SinceDateTime { get; set; }
    }
}

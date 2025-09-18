namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Represents a single error detail
    /// </summary>
    internal class ErrorDetail
    {
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

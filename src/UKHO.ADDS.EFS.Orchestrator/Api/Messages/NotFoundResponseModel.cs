namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Represents a not found (404) error detail
    /// </summary>
    internal class NotFoundResponseModel
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}

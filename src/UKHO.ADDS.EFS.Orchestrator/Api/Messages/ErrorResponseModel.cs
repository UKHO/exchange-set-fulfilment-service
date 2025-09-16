namespace UKHO.ADDS.EFS.Orchestrator.Api.Messages
{
    /// <summary>
    /// Represents a response containing error details for a request
    /// </summary>
    internal class ErrorResponseModel
    {
        public string CorrelationId { get; set; } = string.Empty;
        public List<ErrorDetail> Errors { get; set; } = new();
    }
}

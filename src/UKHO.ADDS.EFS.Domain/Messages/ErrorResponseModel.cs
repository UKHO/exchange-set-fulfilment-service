using System.Collections.Generic;

namespace UKHO.ADDS.EFS.Domain.Messages
{
    /// <summary>
    /// Represents a response containing error details for a request
    /// </summary>
    public class ErrorResponseModel
    {
        public string CorrelationId { get; set; } = string.Empty;
        public List<ErrorDetail> Errors { get; set; } = new();
    }

    /// <summary>
    /// Represents a single error detail
    /// </summary>
    public class ErrorDetail
    {
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

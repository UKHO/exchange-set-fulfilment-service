using UKHO.ADDS.Mocks.Headers;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    /// <summary>
    /// Base class for FSS endpoint mocks providing common error response helpers
    /// </summary>
    public abstract class FssEndpointBase : ServiceEndpointMock
    {
        protected const string InternalServerErrorMessage = "Internal Server Error";

        /// <summary>
        /// Creates a standard error response with correlationId and error details
        /// </summary>
        /// <param name="correlationId">The correlation ID from the request</param>
        /// <param name="source">The source of the error (e.g., "Add File")</param>
        /// <param name="description">The error description</param>
        /// <returns>Anonymous object for JSON serialization</returns>
        protected static object CreateErrorResponse(string correlationId, string source, string description) => new
        {
            correlationId,
            errors = new[]
            {
                new { source, description }
            }
        };

        /// <summary>
        /// Creates a standard details response with correlationId and details
        /// </summary>
        /// <param name="correlationId">The correlation ID from the request</param>
        /// <param name="details">The details message</param>
        /// <returns>Anonymous object for JSON serialization</returns>
        protected static object CreateDetailsResponse(string correlationId, string details) => new
        {
            correlationId,
            details
        };

        /// <summary>
        /// Creates a standard unsupported media type response
        /// </summary>
        /// <returns>Anonymous object for JSON serialization</returns>
        protected static object CreateUnsupportedMediaTypeResponse() => new
        {
            type = "https://example.com",
            title = "Unsupported Media Type",
            status = 415,
            traceId = "00-012-0123-01"
        };

        /// <summary>
        /// Gets the correlation ID from the request headers
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>The correlation ID string</returns>
        protected static string GetCorrelationId(HttpRequest request) =>
            request.Headers[WellKnownHeader.CorrelationId];
    }
}

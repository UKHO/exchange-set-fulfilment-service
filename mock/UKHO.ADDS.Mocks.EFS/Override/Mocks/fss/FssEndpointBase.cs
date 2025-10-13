using UKHO.ADDS.Mocks.Headers;
using UKHO.ADDS.Mocks.States;

namespace UKHO.ADDS.Mocks.EFS.Override.Mocks.fss
{
    /// <summary>
    /// Base class for FSS endpoint mocks providing common error response helpers
    /// </summary>
    public abstract class FssEndpointBase : ServiceEndpointMock
    {
        protected const string InternalServerErrorMessage = "Internal Server Error";
        protected const string S100ExchangeSetsPath = "/S100-ExchangeSets";

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

        /// <summary>
        /// Handles common error states using standard error responses
        /// </summary>
        /// <param name="state">The well-known state string</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="source">The source component name</param>
        /// <param name="badRequestDescription">Custom bad request description</param>
        /// <returns>IResult for the error state, or null if not handled</returns>
        protected static IResult? HandleCommonErrorStates(string state, string correlationId, string source, string badRequestDescription = "Invalid request.")
        {
            return state switch
            {
                WellKnownState.BadRequest => Results.Json(CreateErrorResponse(correlationId, source, badRequestDescription), statusCode: 400),
                WellKnownState.NotFound => Results.Json(CreateDetailsResponse(correlationId, "Not Found"), statusCode: 404),
                WellKnownState.UnsupportedMediaType => Results.Json(CreateUnsupportedMediaTypeResponse(), statusCode: 415),
                WellKnownState.InternalServerError => Results.Json(CreateDetailsResponse(correlationId, InternalServerErrorMessage), statusCode: 500),
                _ => null // Not handled, let the endpoint handle it
            };
        }

        /// <summary>
        /// Common request setup for FSS endpoints - extracts headers and initializes common values
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="response">The HTTP response</param>
        /// <returns>Tuple containing state and correlationId</returns>
        protected (string state, string correlationId) SetupRequest(HttpRequest request, HttpResponse response)
        {
            EchoHeaders(request, response, [WellKnownHeader.CorrelationId]);
            var state = GetState(request);
            var correlationId = GetCorrelationId(request);
            return (state, correlationId);
        }

        /// <summary>
        /// Safely creates the S100-ExchangeSets directory and any subdirectories
        /// </summary>
        /// <param name="subPath">Optional subdirectory path within S100-ExchangeSets</param>
        protected void EnsureS100DirectoryExists(string? subPath = null)
        {
            var fileSystem = GetFileSystem();
            try
            {
                fileSystem.CreateDirectory(S100ExchangeSetsPath);
                if (!string.IsNullOrEmpty(subPath))
                {
                    fileSystem.CreateDirectory($"{S100ExchangeSetsPath}/{subPath}");
                }
            }
            catch (Exception)
            {
                // Ignore directory creation errors
            }
        }

        /// <summary>
        /// Processes common FSS error states with default error handling
        /// </summary>
        /// <param name="state">The current state</param>
        /// <param name="correlationId">The correlation ID</param>
        /// <param name="source">The source component name</param>
        /// <param name="customBadRequestResponse">Custom bad request response, or null to use HandleCommonErrorStates</param>
        /// <returns>IResult for common error states, or null if state should be handled by endpoint</returns>
        protected static IResult? ProcessCommonStates(string state, string correlationId, string source, IResult? customBadRequestResponse = null)
        {
            return state switch
            {
                WellKnownState.BadRequest when customBadRequestResponse != null => customBadRequestResponse,
                WellKnownState.NotFound => Results.Json(CreateDetailsResponse(correlationId, "Not Found"), statusCode: 404),
                WellKnownState.UnsupportedMediaType => Results.Json(CreateUnsupportedMediaTypeResponse(), statusCode: 415),
                WellKnownState.InternalServerError => Results.Json(CreateDetailsResponse(correlationId, InternalServerErrorMessage), statusCode: 500),
                _ when state != WellKnownState.Default => WellKnownStateHandler.HandleWellKnownState(state),
                _ => null // Let endpoint handle Default state and custom BadRequest
            };
        }
    }
}

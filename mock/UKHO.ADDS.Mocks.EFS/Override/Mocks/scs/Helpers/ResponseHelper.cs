using System.Text.Json.Nodes;
using UKHO.ADDS.Mocks.Headers;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace UKHO.ADDS.Mocks.Configuration.Mocks.scs.Helpers
{
    /// <summary>
    /// Constants for standardized error response URIs
    /// </summary>
    public static class ErrorResponseConstants
    {
        /// <summary>
        /// RFC 9110 Section 15.5.16 - Unsupported Media Type
        /// </summary>
        public const string UnsupportedMediaTypeUri = "https://tools.ietf.org/html/rfc9110#section-15.5.16";
        
        /// <summary>
        /// Default fallback URI for generic error responses
        /// </summary>
        public const string GenericErrorUri = "https://example.com";
    }

    /// <summary>
    /// Helper class to reduce duplication in mock endpoint response generation
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// Safely extracts correlation ID from request headers
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <returns>The correlation ID or empty string if not found</returns>
        public static string GetCorrelationId(HttpRequest request)
        {
            return request.Headers.ContainsKey(WellKnownHeader.CorrelationId)
                ? request.Headers[WellKnownHeader.CorrelationId].ToString()
                : string.Empty;
        }

        /// <summary>
        /// Creates a standardized 400 Bad Request response with correlation ID and errors
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="source">The source of the error</param>
        /// <param name="description">The error description</param>
        /// <returns>A 400 Bad Request IResult</returns>
        public static IResult CreateBadRequestResponse(HttpRequest request, string source, string description)
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                errors = new[]
                {
                    new
                    {
                        source,
                        description
                    }
                }
            }, statusCode: 400);
        }

        /// <summary>
        /// Creates a standardized 404 Not Found response with correlation ID
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="details">Optional details for the not found response</param>
        /// <returns>A 404 Not Found IResult</returns>
        public static IResult CreateNotFoundResponse(HttpRequest request, string details = "Not Found")
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                details
            }, statusCode: 404);
        }

        /// <summary>
        /// Creates a standardized 415 Unsupported Media Type response
        /// </summary>
        /// <param name="typeUri">The RFC URI for the error type</param>
        /// <param name="traceId">Optional trace ID</param>
        /// <returns>A 415 Unsupported Media Type IResult</returns>
        public static IResult CreateUnsupportedMediaTypeResponse(
            string? typeUri = null, 
            string? traceId = null)
        {
            return Results.Json(new
            {
                type = typeUri ?? ErrorResponseConstants.UnsupportedMediaTypeUri,
                title = "Unsupported Media Type",
                status = 415,
                traceId = traceId ?? Guid.NewGuid().ToString("D")[..23]
            }, statusCode: 415);
        }

        /// <summary>
        /// Creates a standardized 500 Internal Server Error response with correlation ID
        /// </summary>
        /// <param name="request">The HTTP request</param>
        /// <param name="details">Optional details for the error</param>
        /// <returns>A 500 Internal Server Error IResult</returns>
        public static IResult CreateInternalServerErrorResponse(HttpRequest request, string details = "Internal Server Error")
        {
            return Results.Json(new
            {
                correlationId = GetCorrelationId(request),
                details
            }, statusCode: 500);
        }
    }
}

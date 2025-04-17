using System.Net;
using Microsoft.AspNetCore.Mvc;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;

namespace UKHO.ADDS.EFS.Orchestrator.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (OrchestratorException exception)
            {
                await HandleExceptionAsync(httpContext, exception, exception.Message, exception.MessageArguments);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(httpContext, exception, exception.Message);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception, string message, params object[] messageArgs)
        {
            httpContext.Response.ContentType = ApiHeaderKeys.ContentType;
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogError(exception, message, messageArgs);

            var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault()!;

            var problemDetails = new ProblemDetails
            {
                Extensions =
                {
                    ["correlationId"] = correlationId
                }
            };
            httpContext.Response.Headers.Append(ApiHeaderKeys.OriginHeaderKey, "Orchestrator");
            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception, EventId eventId, string message, params object[] messageArgs)
        {
            httpContext.Response.ContentType = ApiHeaderKeys.ContentType;
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogError(eventId, exception, message, messageArgs);

            var correlationId = httpContext.Request.Headers[ApiHeaderKeys.XCorrelationIdHeaderKey].FirstOrDefault()!;

            var problemDetails = new ProblemDetails
            {
                Extensions =
                {
                    ["correlationId"] = correlationId
                }
            };
            httpContext.Response.Headers.Append(ApiHeaderKeys.OriginHeaderKey, "Orchestrator");
            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

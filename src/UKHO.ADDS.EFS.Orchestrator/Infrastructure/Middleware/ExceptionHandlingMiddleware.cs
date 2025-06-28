using System.Net;
using Microsoft.AspNetCore.Mvc;
using UKHO.ADDS.EFS.Constants;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Extensions;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Middleware
{
    internal class ExceptionHandlingMiddleware
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

            var errorMessage = messageArgs.Length > 0 ? string.Format(message, messageArgs) : message;

            _logger.LogUnhandledHttpError(errorMessage, exception);

            var correlationId = httpContext.GetCorrelationId();

            var problemDetails = new ProblemDetails
            {
                Extensions =
                {
                    ["correlationId"] = correlationId
                }
            };

            httpContext.Response.Headers.Append(ApiHeaderKeys.OriginHeaderKey, "EFS Orchestrator");
            await httpContext.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

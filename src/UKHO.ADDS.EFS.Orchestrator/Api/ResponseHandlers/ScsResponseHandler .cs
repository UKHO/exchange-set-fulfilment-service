using System.Net;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers
{

    public class ScsResponseHandler : IScsResponseHandler
    {
        public const string ScsServiceName = "SCS";
        public IResult HandleScsResponse(AssemblyPipelineResponse result, string requestType, ILogger logger, HttpContext httpContext)
        {
            if (result is null)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            AppendErrorOriginHeadersIfNeeded(result, httpContext);

            switch (result.ScsResponseCode)
            {
                case HttpStatusCode.OK:
                    AppendLastModifiedHeader(httpContext, result.ScsLastModified);
                    return Results.Accepted(null, result.Response);

                case HttpStatusCode.NotModified:
                    AppendLastModifiedHeader(httpContext, result.ScsLastModified);

                    return result.BuildStatus == Domain.Builds.BuildState.Scheduled
                        ? Results.Accepted(null, result.Response)
                        : Results.StatusCode(StatusCodes.Status304NotModified);

                default:
                    logger.LogSalesCatalogueServiceFailed(requestType, (int)result.ScsResponseCode);
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static void AppendErrorOriginHeadersIfNeeded(AssemblyPipelineResponse result, HttpContext httpContext)
        {
            var scsStatus = (int)result.ScsResponseCode;

            if (scsStatus < 400 || scsStatus > 599)
                return;

            httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginHeaderKey] = ScsServiceName;
            httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginStatusHeaderKey] = scsStatus.ToString();
        }

        private static void AppendLastModifiedHeader(HttpContext httpContext, DateTime? lastModified)
        {
            if (!lastModified.HasValue)
            {
                return;
            }

            var formatted = lastModified.Value.ToUniversalTime().ToString("R");
            if (!string.IsNullOrEmpty(formatted))
            {
                httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey] = formatted;
            }
        }
    }
}

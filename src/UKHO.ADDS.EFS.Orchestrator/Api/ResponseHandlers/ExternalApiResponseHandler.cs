using System.Net;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers
{

    public class ExternalApiResponseHandler : IExternalApiResponseHandler
    {
        public IResult HandleExternalApiResponse(AssemblyPipelineResponse result, string requestType, ILogger logger, HttpContext httpContext)
        {
            if (result is null)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            AppendErrorOriginHeadersIfNeeded(result, httpContext);

            switch (result.ExternalApiResponseCode)
            {
                case HttpStatusCode.OK:
                    AppendLastModifiedHeader(httpContext, result.LastModified);
                    return Results.Accepted(null, result.Response);

                case HttpStatusCode.NotModified:
                    AppendLastModifiedHeader(httpContext, result.LastModified);

                    return result.BuildStatus == Domain.Builds.BuildState.Scheduled
                        ? Results.Accepted(null, result.Response)
                        : Results.StatusCode(StatusCodes.Status304NotModified);

                default:
                    logger.LogSalesCatalogueServiceFailed(requestType, (int)result.ExternalApiResponseCode);
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static void AppendErrorOriginHeadersIfNeeded(AssemblyPipelineResponse result, HttpContext httpContext)
        {
            var externalApiStatus = (int)result.ExternalApiResponseCode;

            if (externalApiStatus < 400 || externalApiStatus > 599)
                return;

            httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginHeaderKey] = result.ExternalApiServiceName.ToString();
            httpContext.Response.Headers[ApiHeaderKeys.ErrorOriginStatusHeaderKey] = externalApiStatus.ToString();
        }

        private static void AppendLastModifiedHeader(HttpContext httpContext, DateTime? lastModified)
        {
            if (!lastModified.HasValue)
            {
                return;
            }

            var lastModifiedHeader = lastModified.Value.ToUniversalTime().ToString("R");
            if (!string.IsNullOrEmpty(lastModifiedHeader))
            {
                httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey] = lastModifiedHeader;
            }
        }
    }
}

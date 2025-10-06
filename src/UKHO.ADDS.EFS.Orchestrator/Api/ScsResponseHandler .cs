using System.Net;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public class ScsResponseHandler : IScsResponseHandler
    {
        public IResult HandleScsResponse(AssemblyPipelineResponse result, CorrelationId correlationId, ILogger logger, HttpContext httpContext)
        {
            if(result.BuildStatus!= Domain.Builds.BuildState.Scheduled )
            {
                httpContext.Response.Headers.Append(ApiHeaderKeys.ErrorOriginHeaderKey, result.ErrorOrigin);
                httpContext.Response.Headers.Append(ApiHeaderKeys.ErrorOriginStatusHeaderKey, ((int)result.ScsResponseCode).ToString());
            }

            switch (result.ScsResponseCode)
            {
                case HttpStatusCode.OK:
                    AppendLastModifiedHeader(httpContext, result.ScsLastModified);
                    return Results.Accepted(null, result.Response);

                case HttpStatusCode.NotModified:
                    if(result.BuildStatus == Domain.Builds.BuildState.Scheduled)
                    {
                        AppendLastModifiedHeader(httpContext, result.ScsLastModified);
                        return Results.Accepted(null, result.Response);
                    }
                    else
                    {
                        AppendLastModifiedHeader(httpContext, result.ScsLastModified);
                        return Results.StatusCode(304);
                    }                        
                    
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.UnsupportedMediaType:
                    logger.LogSalesCatalogueServiceFailed((int)result.ScsResponseCode, correlationId.Value, result.ErrorOrigin);
                    return Results.StatusCode(500);

                default:
                    logger.LogSalesCatalogueServiceFailed((int)result.ScsResponseCode, correlationId.Value, result.ErrorOrigin);
                    return Results.StatusCode(500);
            }


        }

        private static void AppendLastModifiedHeader(HttpContext httpContext, DateTime? lastModified)
        {
            if (lastModified.HasValue)
            {
                var formatted = lastModified.Value.ToUniversalTime().ToString("R");
                if (!string.IsNullOrEmpty(formatted))
                {
                    httpContext.Response.Headers[ApiHeaderKeys.LastModifiedHeaderKey] = formatted;
                }
            }
        }

        
    }

}

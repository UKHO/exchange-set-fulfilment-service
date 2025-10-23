using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers
{
    public interface IExternalApiResponseHandler
    {
        IResult HandleExternalApiResponse(AssemblyPipelineResponse result, string requestType, ILogger logger, HttpContext httpContext);
    }

}

using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api.ResponseHandlers
{
    public interface IScsResponseHandler
    {
        IResult HandleScsResponse(AssemblyPipelineResponse result, string requestType, ILogger logger, HttpContext httpContext);
    }

}

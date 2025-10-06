using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Api
{
    public interface IScsResponseHandler
    {
        IResult HandleScsResponse(AssemblyPipelineResponse result, CorrelationId correlationId, ILogger logger, HttpContext httpContext);
    }

}

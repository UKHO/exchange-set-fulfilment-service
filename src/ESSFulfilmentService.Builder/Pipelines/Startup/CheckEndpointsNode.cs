using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace ESSFulfilmentService.Builder.Pipelines.Startup
{
    internal class CheckEndpointsNode : Node<StartupPipelineContext>
    {
        // verify fss, scs, service bus
    }
}

using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace ESSFulfilmentService.Builder.Pipelines.Startup
{
    internal class ReadConfigurationNode : Node<StartupPipelineContext>
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<StartupPipelineContext> context)
        {
            return NodeResultStatus.Succeeded;
        }
    }
}

using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Create
{
    internal class TestCreateNode : S63ExchangeSetPipelineNode
    {
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetPipelineContext> context)
        {
            return NodeResultStatus.Succeeded;
        }
    }
}

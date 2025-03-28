using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class QuerySalesCatalogueNode : ExchangeSetPipelineNode
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context) => Task.FromResult(NodeResultStatus.NotRun);
    }
}

using UKHO.Infrastructure.Pipelines.Nodes;
using UKHO.Infrastructure.Pipelines;

namespace UKHO.ExchangeSets.Fulfilment.Nodes.Builder
{
    internal class CreateExchangeSetContainerNode : Node<ExchangeSetBuilderContext>
    {
        public override Task<bool> ShouldExecuteAsync(IExecutionContext<ExchangeSetBuilderContext> context)
        {
            return Task.FromResult(true);
        }

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetBuilderContext> context)
        {
            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }
}

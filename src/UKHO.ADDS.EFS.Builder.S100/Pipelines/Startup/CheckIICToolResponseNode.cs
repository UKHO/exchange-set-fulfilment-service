using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckIICToolResponseNode : ExchangeSetPipelineNode
    {
        private const string ExchangeSetId = "es06";

        public CheckIICToolResponseNode()
        {
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            await context.Subject.ToolClient.AddExchangeSetAsync(context.Subject.WorkspaceRootPath, ExchangeSetId);
            await context.Subject.ToolClient.AddContentAsync(context.Subject.WorkspaceRootPath, ExchangeSetId);
            await context.Subject.ToolClient.SignExchangeSetAsync(context.Subject.WorkspaceRootPath, ExchangeSetId);
            await context.Subject.ToolClient.ExtractExchangeSetAsync(context.Subject.WorkspaceRootPath, ExchangeSetId);

            return NodeResultStatus.Succeeded;
        }
    }
}

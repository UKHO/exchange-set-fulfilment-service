using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup
{
    internal class CheckIICToolResponseNode : ExchangeSetPipelineNode
    {
        private const string ExchangeSetId = "es07";

        public CheckIICToolResponseNode()
        {
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            await context.Subject.ToolClient.AddExchangeSetAsync(ExchangeSetId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
            await context.Subject.ToolClient.AddContentAsync(ExchangeSetId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
            await context.Subject.ToolClient.SignExchangeSetAsync(ExchangeSetId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
            await context.Subject.ToolClient.ExtractExchangeSetAsync(ExchangeSetId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);

            return NodeResultStatus.Succeeded;
        }
    }
}

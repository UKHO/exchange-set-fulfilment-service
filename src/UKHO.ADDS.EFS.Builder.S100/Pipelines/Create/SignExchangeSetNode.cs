using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for signing the exchange set using the provided tool client.
    /// </summary>
    internal class SignExchangeSetNode : ExchangeSetPipelineNode
    {
        /// <summary>
        /// Executes the node logic to sign the exchange set.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<SignExchangeSetNode>();

            var result = await context.Subject.ToolClient.SignExchangeSetAsync(context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);

            if (!result.IsSuccess(out _, out var error))
            {
                logger.LogSignExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

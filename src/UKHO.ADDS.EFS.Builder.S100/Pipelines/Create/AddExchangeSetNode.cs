using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for adding an exchange set using the provided tool client.
    /// </summary>
    internal class AddExchangeSetNode : ExchangeSetPipelineNode
    {
        /// <summary>
        /// Executes the node logic to add an exchange set.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddExchangeSetNode>();

            var result = await context.Subject.ToolClient.AddExchangeSetAsync(
                context.Subject.JobId,
                context.Subject.WorkspaceAuthenticationKey,
                context.Subject.Job.GetCorrelationId()
            );

            if (!result.IsSuccess(out _, out var error))
            {
                logger.LogAddExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

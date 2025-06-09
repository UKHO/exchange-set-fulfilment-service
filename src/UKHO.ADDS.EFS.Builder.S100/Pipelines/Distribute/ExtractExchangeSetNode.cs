using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class ExtractExchangeSetNode : ExchangeSetPipelineNode
    {
        private ILogger _logger;

        /// <summary>
        /// Executes the node asynchronously within the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing pipeline and job information.</param>
        /// <returns>NodeResultStatus indicating whether the node execution succeeded or failed</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<ExtractExchangeSetNode>();
            try
            {
                var result = await context.Subject.ToolClient.ExtractExchangeSetAsync(
                    context.Subject.Job?.Id!,
                    context.Subject.WorkspaceAuthenticationKey,
                    context.Subject.Job?.CorrelationId!,
                    context.Subject.ExchangeSetArchiveFolderName);

                if (result.IsFailure(out var error, out var _))
                {
                    _logger.LogIICExtractExchangeSetError(error);
                    return NodeResultStatus.Failed;
                }
                else
                {
                    return NodeResultStatus.Succeeded;
                }
            }
            catch (Exception ex)
            {
                _logger.LogExtractExchangeSetNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }
    }
}

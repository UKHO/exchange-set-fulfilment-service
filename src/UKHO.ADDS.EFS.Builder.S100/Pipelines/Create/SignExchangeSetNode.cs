using UKHO.ADDS.EFS.Builder.S100.IIC;
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
        // Tool client used to interact with the external service for signing operations.
        private readonly IToolClient _toolClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignExchangeSetNode"/> class.
        /// </summary>
        /// <param name="toolClient">The tool client to use for signing operations.</param>
        /// <exception cref="ArgumentException">Thrown if toolClient is null.</exception>
        public SignExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentException(nameof(toolClient));
        }

        /// <summary>
        /// Executes the node logic to sign the exchange set.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<SignExchangeSetNode>();

            var result = await _toolClient.SignExchangeSetAsync(context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
                
            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogSignExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

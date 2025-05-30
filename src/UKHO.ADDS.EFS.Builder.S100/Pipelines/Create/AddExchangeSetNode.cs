using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for adding an exchange set using the provided tool client.
    /// </summary>
    internal class AddExchangeSetNode : ExchangeSetPipelineNode
    {
        // Tool client used to interact with the external service for exchange set operations.
        private readonly IToolClient _toolClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddExchangeSetNode"/> class.
        /// </summary>
        /// <param name="toolClient">The tool client to use for exchange set operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if toolClient is null.</exception>
        public AddExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        /// <summary>
        /// Executes the node logic to add an exchange set.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddExchangeSetNode>();

            var result = await _toolClient.AddExchangeSetAsync(
                context.Subject.JobId,
                context.Subject.WorkspaceAuthenticationKey,
                context.Subject.Job.CorrelationId
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

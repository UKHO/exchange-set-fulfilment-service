using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for adding content to an exchange set using the provided tool client.
    /// </summary>
    internal class AddContentExchangeSetNode : ExchangeSetPipelineNode
    {
        // Tool client used to interact with the external service for content operations.
        private readonly IToolClient _toolClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddContentExchangeSetNode"/> class.
        /// </summary>
        /// <param name="toolClient">The tool client to use for content operations.</param>
        /// <exception cref="ArgumentException">Thrown if toolClient is null.</exception>
        public AddContentExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentException(nameof(toolClient));
        }

        /// <summary>
        /// Executes the node logic to add content to the exchange set.
        /// </summary>
        /// <param name="context">The pipeline execution context.</param>
        /// <returns>The result status of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddContentExchangeSetNode>();


            var result = await _toolClient.AddContentAsync(context.Subject.WorkSpaceSpoolDataSetFilesPath, context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.JobId);

            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogAddContentExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            var result1 = await _toolClient.AddContentAsync(context.Subject.WorkSpaceSpoolSupportFilesPath, context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.JobId);

            if (!result1.IsSuccess(out var value1, out var error1))
            {
                logger.LogAddContentExchangeSetNodeFailed(error1);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

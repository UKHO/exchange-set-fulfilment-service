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
        /// Executes the node logic to add content to the exchange set by processing the dataset and support files paths.
        /// Returns <see cref="NodeResultStatus.Succeeded"/> if all content is added successfully; otherwise, returns <see cref="NodeResultStatus.Failed"/>.
        /// </summary>
        /// <param name="context">The pipeline execution context containing job and workspace information.</param>
        /// <returns>A <see cref="NodeResultStatus"/> indicating the result of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddContentExchangeSetNode>();
            var jobId = context.Subject.JobId;
            var authKey = context.Subject.WorkspaceAuthenticationKey;

            // Define paths to process
            var contentPaths = new[]
            {
                context.Subject.WorkSpaceSpoolDataSetFilesPath,
                context.Subject.WorkSpaceSpoolSupportFilesPath
            };

            // Process each path
            foreach (var path in contentPaths)
            {
                if (!await AddContentForPathAsync(path, jobId, authKey, logger))
                {
                    return NodeResultStatus.Failed;
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private async Task<bool> AddContentForPathAsync(string path, string jobId, string authKey, ILogger logger)
        {
            var result = await _toolClient.AddContentAsync(path, jobId, authKey, jobId);

            if (result.IsSuccess(out _, out var error))
            {
                return true;
            }

            logger.LogAddContentExchangeSetNodeFailed(error);
            return false;
        }
    }
}

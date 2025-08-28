using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for adding content to an exchange set using the provided tool client.
    /// </summary>
    internal class AddContentExchangeSetNode : S100ExchangeSetPipelineNode
    {
        /// <summary>
        /// Executes the node logic to add content to the exchange set by processing the dataset and support files paths.
        /// Returns <see cref="NodeResultStatus.Succeeded"/> if all content is added successfully; otherwise, returns <see cref="NodeResultStatus.Failed"/>.
        /// </summary>
        /// <param name="context">The pipeline execution context containing job and workspace information.</param>
        /// <returns>A <see cref="NodeResultStatus"/> indicating the result of the node execution.</returns>
        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddContentExchangeSetNode>();
            var jobId = context.Subject.JobId;
            var authKey = context.Subject.WorkspaceAuthenticationKey;
            var toolClient = context.Subject.ToolClient;

            // Define paths to process
            var contentPaths = new[]
            {
                context.Subject.WorkSpaceSpoolDataSetFilesPath,
                context.Subject.WorkSpaceSpoolSupportFilesPath
            };

            // Process each path
            foreach (var path in contentPaths)
            {
                if (!await AddContentForPathAsync(toolClient, path, jobId, authKey, logger))
                {
                    return NodeResultStatus.Failed;
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private async Task<bool> AddContentForPathAsync(IToolClient toolClient, string path, JobId jobId, string authKey, ILogger logger)
        {
            var result = await toolClient.AddContentAsync(path, jobId, authKey);

            if (result.IsSuccess(out _, out var error))
            {
                return true;
            }

            logger.LogAddContentExchangeSetNodeFailed(error);
            return false;
        }
    }
}

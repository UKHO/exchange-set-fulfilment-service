using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    /// <summary>
    /// Pipeline node responsible for adding content to an exchange set using the provided tool client.
    /// </summary>
    internal class AddContentExchangeSetNode : S100ExchangeSetPipelineNode
    {
        public override Task<bool> ShouldExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
        {
            return Task.FromResult(context.Subject.BatchFileNameDetails != null);
        }

        /// <summary>
        /// Executes the node logic to add content to the exchange set by processing dataset and support files paths.
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
            var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);
           
            var catalogPaths = GetCatalogPaths(context, downloadPath);

            // Process each catalog path
            foreach (var catalogPath in catalogPaths)
            {
                if (!await AddContentForPathAsync(toolClient, catalogPath, jobId, authKey, logger))
                {
                    return NodeResultStatus.Failed;
                }
            }

            return NodeResultStatus.Succeeded;
        }

        private static List<string> GetCatalogPaths(IExecutionContext<S100ExchangeSetPipelineContext> context, string downloadPath)
        {
            var batchFileNameDetails = context.Subject.BatchFileNameDetails;
            var catalogPaths = new List<string>();

            foreach (var folderName in batchFileNameDetails)
            {
                var fullFolderPath = Path.Combine(downloadPath, folderName);

                if (!Directory.Exists(fullFolderPath))
                {
                    return [];
                }

                var catalogPath = folderName + "/S100_ROOT/CATALOG.XML";
                catalogPaths.Add(catalogPath);
            }

            return catalogPaths;
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

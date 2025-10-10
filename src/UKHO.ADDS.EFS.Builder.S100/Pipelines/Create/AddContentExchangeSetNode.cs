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
            
            var catalogPaths = GetCatalogPaths(context.Subject, downloadPath);

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

        /// <summary>
        /// Gets all catalog paths that need to be processed from the workspace.
        /// </summary>
        /// <param name="context">The pipeline context containing workspace information.</param>
        /// <param name="downloadPath">The download path where content folders are located.</param>
        /// <returns>A list of catalog paths relative to the workspace that need to be processed.</returns>
        private static List<string> GetCatalogPaths(S100ExchangeSetPipelineContext context, string downloadPath)
        {
            if (!Directory.Exists(downloadPath))
            {
                return [];
            }

            var catalogPaths = new List<string>();
            
            var folderNames = Directory.GetDirectories(downloadPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && name.Length > 5)
                .ToList();

            foreach (var folderName in folderNames)
            {
                var fullFolderPath = Path.Combine(downloadPath, folderName);

                if (Directory.Exists(fullFolderPath))
                {
                    var catalogPath = folderName + "/S100_ROOT/CATALOG.XML";
                    catalogPaths.Add(catalogPath);
                }
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

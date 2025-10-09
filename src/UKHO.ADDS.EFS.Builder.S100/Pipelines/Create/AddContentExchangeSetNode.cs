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
            var catalogPaths = new List<string>();

            var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);
            var longFolderNames = GetLongFolderNames(downloadPath);

            foreach (var folderName in longFolderNames)
            {
                var filesPath = BuildWorkspacePath(context.Subject, folderName);

                var validContentPaths = new[] {
                    (Path: folderName, FullPath: filesPath)
                }
               .Where(x => Directory.Exists(x.FullPath))
               .Select(x => x.Path)
               .ToArray();

                foreach (var path in validContentPaths)
                {
                    var catalogPath = path + "/S100_ROOT/CATALOG.XML";
                    catalogPaths.Add(catalogPath);
                }
            }

            // Process each path
            foreach (var path in catalogPaths)
            {
                if (!await AddContentForPathAsync(toolClient, path, jobId, authKey, logger))
                {
                    return NodeResultStatus.Failed;
                }
            }

            return NodeResultStatus.Succeeded;
        }

        /// <summary>
        /// Gets a list of folder names in the specified path with length greater than 5 characters.
        /// </summary>
        /// <param name="directoryPath">The directory path to search for folders.</param>
        /// <returns>A list of folder names with length greater than 5 characters.</returns>
        private static List<string> GetLongFolderNames(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return [];
            }

            return Directory.GetDirectories(directoryPath)
                .Select(Path.GetFileName)
                .Where(name => name != null && name.Length > 5)
                .ToList();
        }

        /// <summary>
        /// Builds a full workspace path by combining the root path, spool path, and the specified sub-path.
        /// </summary>
        /// <param name="context">The pipeline context containing workspace path information.</param>
        /// <param name="subPath">The sub-path to combine with the workspace root and spool paths.</param>
        /// <returns>A fully qualified workspace path.</returns>
        private static string BuildWorkspacePath(S100ExchangeSetPipelineContext context, string subPath)
        {
            return Path.Combine(context.WorkSpaceRootPath, context.WorkSpaceSpoolPath, subPath);
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

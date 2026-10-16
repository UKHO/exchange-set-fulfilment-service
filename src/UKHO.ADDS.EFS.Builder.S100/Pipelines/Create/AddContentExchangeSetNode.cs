using System.Linq;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

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
            var workspaceName = context.Subject.WorkspaceName;
            var authKey = context.Subject.WorkspaceAuthenticationKey;
            var toolClient = context.Subject.ToolClient;
            var downloadPath = Path.Combine(context.Subject.WorkSpaceRootPath, context.Subject.WorkSpaceSpoolPath);

            logger.LogAddContentExchangeSetNodeStartedDiagnostic((string)jobId, downloadPath);

            var catalogPaths = GetCatalogPaths(context, downloadPath, logger);

            // Process each catalog path
            var successfulAdds = 0;
            foreach (var catalogPath in catalogPaths)
            {
                logger.LogAddContentExchangeSetNodeAddingCatalogPathDiagnostic(catalogPath, (string)jobId);
                if (!await AddContentForPathAsync(toolClient, catalogPath, jobId, workspaceName, authKey, logger))
                {
                    logger.LogAddContentExchangeSetNodeCatalogPathAddFailedDiagnostic(catalogPath, (string)jobId);
                    return NodeResultStatus.Failed;
                }

                successfulAdds++;
            }

            logger.LogAddContentExchangeSetNodeCompletedDiagnostic((string)jobId, successfulAdds, catalogPaths.Count);

            return NodeResultStatus.Succeeded;
        }

        private static List<string> GetCatalogPaths(IExecutionContext<S100ExchangeSetPipelineContext> context, string downloadPath, ILogger logger)
        {
            var batchFileNameDetails = context.Subject.BatchFileNameDetails;
            var catalogPaths = new List<string>();

            logger.LogAddContentExchangeSetNodeResolvingCatalogPathsDiagnostic(batchFileNameDetails.Count(), downloadPath);

            foreach (var folderName in batchFileNameDetails)
            {
                var fullFolderPath = Path.Combine(downloadPath, folderName);

                if (!Directory.Exists(fullFolderPath))
                {
                    logger.LogAddContentExchangeSetNodeBatchFolderMissingDiagnostic(fullFolderPath);
                    return [];
                }

                var catalogPath = folderName + "/S100_ROOT/CATALOG.XML";
                var fullCatalogPath = Path.Combine(downloadPath, folderName, "S100_ROOT", "CATALOG.XML");

                if (!File.Exists(fullCatalogPath))
                {
                    logger.LogAddContentExchangeSetNodeCatalogMissingDiagnostic(fullCatalogPath, catalogPath);
                }

                catalogPaths.Add(catalogPath);
            }

            return catalogPaths;
        }

        private static async Task<bool> AddContentForPathAsync(IToolClient toolClient, string path, JobId jobId, string workspaceName, string authKey, ILogger logger)
        {
            var result = await toolClient.AddContentAsync(path, jobId, workspaceName, authKey);
            OperationResponse? operationResponse = null;
            IError? error = null;

            if (result.IsSuccess(out operationResponse, out error))
            {
                if (operationResponse is not null)
                {
                    logger.LogAddContentExchangeSetNodeAddContentResponseDiagnostic(path, operationResponse.Code, operationResponse.Type, operationResponse.Message);
                }
                return true;
            }

            logger.LogAddContentExchangeSetNodeFailed(error!);
            return false;
        }
    }
}

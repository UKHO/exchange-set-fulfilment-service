using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class DownloadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;

        public DownloadFilesNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            try
            {
                var products = context.Subject.BatchDetails;

                foreach (var product in products)
                {
                    foreach (var file in product.Files)
                    {
                        var fileName = file.Filename;

                        var httpResponse = await _fileShareReadOnlyClient.DownloadFileAsync(product.BatchId, fileName);
                        var downloadPath = @"/usr/local/tomcat/ROOT/spool/fssdata";
                        //const string downloadPath = @"CopyToFolder";

                        if (!Directory.Exists(downloadPath))
                        {
                            Directory.CreateDirectory(downloadPath);
                        }

                        var path = Path.Combine(downloadPath, fileName);
                        httpResponse.IsSuccess(out var value, out var error);
                        if (value != null)
                        {
                            await using var outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                            await value.CopyToAsync(outputFileStream);
                        }
                    }
                }
                return NodeResultStatus.Succeeded;
            }
            catch (Exception e)
            {
                return NodeResultStatus.Failed;
            }
        }
    }
}

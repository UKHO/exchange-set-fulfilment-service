using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class DownloadFilesNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;
        private const string DownloadPath = @"/usr/local/tomcat/ROOT/spool/fssdata";
        private const long FileSizeInBytes = 10485750;

        public DownloadFilesNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<DownloadFilesNode>();

            var products = context.Subject.Job?.Products;
            var batchDetails = context.Subject.BatchDetails;
            var productList = new List<string>();

            try
            {
                if (products == null || products.Count == 0 || batchDetails == null || !batchDetails.Any())
                {
                    return NodeResultStatus.NotRun;
                }

                foreach (var product in products)
                {
                    var latestPublishBatch = batchDetails
                        .Where(b =>
                            b.Attributes.Any(a => a.Key == "ProductName" && a.Value == product.ProductName) &&
                            b.Attributes.Any(a => a.Key == "UpdateNumber" && a.Value == product.LatestUpdateNumber.ToString()) &&
                            b.Attributes.Any(a => a.Key == "EditionNumber" && a.Value == product.LatestEditionNumber.ToString()))
                        .OrderByDescending(b => b.BatchPublishedDate)
                        .FirstOrDefault();

                    if (latestPublishBatch == null)
                    {
                        return NodeResultStatus.Failed;
                    }

                    var compareProducts = $"{product.ProductName}|{product.LatestEditionNumber}|{product.LatestUpdateNumber}";
                    if (!productList.Contains(compareProducts))
                    {
                        foreach (var file in latestPublishBatch.Files)
                        {
                            var fileName = file.Filename;
                            if (!Directory.Exists(DownloadPath))
                            {
                                Directory.CreateDirectory(DownloadPath);
                            }

                            var path = Path.Combine(DownloadPath, fileName);
                            await using var outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                            var httpResponse = await _fileShareReadOnlyClient.DownloadFileAsync(latestPublishBatch.BatchId, fileName, outputFileStream, context.Subject.Job?.CorrelationId!, FileSizeInBytes);

                            if (httpResponse.IsFailure(out var error, out var value))
                            {
                                var listUpdateNumbers = new List<int?>
                                {
                                    product.LatestUpdateNumber
                                };
                                var batchProduct = new SearchBatchProducts 
                                {
                                    ProductName = product.ProductName!,
                                    EditionNumber = product.LatestEditionNumber,
                                    UpdateNumbers = listUpdateNumbers
                                };
                                var downloadFilesLogView=new DownloadFilesLogView
                                {
                                    BatchId = latestPublishBatch.BatchId,
                                    Product=batchProduct,
                                    FileName = fileName,
                                    CorrelationId= context.Subject.Job?.CorrelationId!,
                                    Error = string.IsNullOrEmpty(error?.Message) ? string.Empty : error.Message
                                };
                                _logger.LogDownloadFilesNodeFssDownloadFailed(downloadFilesLogView);
                                return NodeResultStatus.Failed;
                            }                              
                        }
                    }
                }
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogDownloadFilesNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }
    }
}

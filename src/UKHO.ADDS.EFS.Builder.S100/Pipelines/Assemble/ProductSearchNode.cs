using System.Text;
using System.Web;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class ProductSearchNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;
        private const int DefaultSplitSize = 30;

        private const string ProductNameQueryClause = "$batch(ProductName) eq '{0}' and ";
        private const string EditionNumberQueryClause = "$batch(EditionNumber) eq '{0}' and ";
        private const string UpdateNumberQueryClause = "$batch(UpdateNumber) eq '{0}' ";
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductTypeQueryClause = "$batch(ProductType) eq 'S-100' and ";
        private const int MaxParallelSearchOperations = 5;
        private const int UpdateNumberLimit = 5;
        private const int ProductLimit = 4;
        private const int Limit = 100;
        private const int Start = 0;

        public ProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient) : base()
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            try
            {
                _logger = context.Subject.LoggerFactory.CreateLogger<ProductSearchNode>();

                var products = context.Subject.Job?.Products;
                if (products == null || products.Count == 0)
                {
                    return NodeResultStatus.NotRun;
                }

                var batchList = new List<BatchDetails>();
                var groupedProducts = products
                    .GroupBy(p => p.ProductName)
                    .Select(g => new SearchBatchProducts
                    {
                        ProductName = g.Key,
                        EditionNumber = g.First().LatestEditionNumber,
                        UpdateNumbers = g.Select(p => p.LatestUpdateNumber).ToList()
                    }).ToList();

                var productGroupCount = (int)Math.Ceiling((double)products.Count / MaxParallelSearchOperations);
                var productsList = SplitList(groupedProducts, productGroupCount);

                foreach (var productGroup in productsList)
                {
                    var batchDetails = await QueryFileShareServiceFilesAsync(productGroup, context.Subject.Job?.CorrelationId!);
                    if (batchDetails != null)
                    {
                        batchList.AddRange(batchDetails);
                    }
                }
                context.Subject.BatchDetails = batchList;
                return NodeResultStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogProductSearchNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        private async Task<List<BatchDetails>> QueryFileShareServiceFilesAsync(List<SearchBatchProducts> products, string correlationId)
        {
            var batchDetails = new List<BatchDetails>();
            if (products == null || products.Count == 0)
            {
                return batchDetails;
            }

            var batchProducts = ChunkProductsByProductLimit(products);
            foreach (var productBatch in batchProducts)
            {
                var result = await FetchBatchDetailsForProductsAsync(productBatch, correlationId);
                batchDetails.AddRange(result.Entries);
            }

            return batchDetails;
        }

        private async Task<BatchSearchResponse> FetchBatchDetailsForProductsAsync(List<SearchBatchProducts> products,
             string correlationId)
        {
            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };
            if (products == null || products.Count == 0)
            {
                return batchSearchResponse;
            }

            var productQuery = GenerateQueryForFss(products);
            var totalUpdateCount = products.Sum(p => p.UpdateNumbers.ToList().Count);
            var queryCount = 0;
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductTypeQueryClause} {productQuery}";
            var limit = Limit;
            var start = Start;
            do
            {
                queryCount++;
                var result = await _fileShareReadOnlyClient.SearchAsync(filter, limit, start, correlationId);
                if (result.IsSuccess(out var value, out var error))
                {
                    if (value.Entries.Count != 0)
                    {
                        foreach (var item in value.Entries)
                        {
                            //if (cancellationToken.IsCancellationRequested)
                            //{
                            //    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                            //    throw new OperationCanceledException();
                            //}
                            //foreach (var productItem in products)
                            //{
                            //    var matchProduct = item.Attributes.Where(a => a.Key == "UpdateNumber");
                            //    var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
                            //    var compareProducts = $"{productItem.ProductName}|{productItem.EditionNumber}|{updateNumber}";
                            //    if (!productList.Contains(compareProducts))
                            //    {
                            //await CheckProductOrCancellationData(internalSearchBatchResponse, productList, item, productItem, updateNumber, compareProducts, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                            await CheckProductOrCancellationData(item);
                            //}
                            //}
                        }
                        batchSearchResponse.Entries.AddRange(value.Entries);
                    }

                    var queryString = value.Links?.Next?.Href;
                    if (!string.IsNullOrEmpty(queryString))
                    {
                        var parsedValues = ParseQueryString(queryString);
                        limit = parsedValues.TryGetValue("limit", out var urlLimit) ? int.Parse(urlLimit) : limit;
                        start = parsedValues.TryGetValue("start", out var urlStart) ? int.Parse(urlStart) : start;
                        filter = parsedValues.TryGetValue("$filter", out var urlFilter) ? urlFilter : filter;
                    }
                    else
                    {
                        filter = string.Empty;
                    }
                }
                else
                {
                    //TODO: - log search query of unsuccessful response from fss
                    _logger.LogProductSearchNodeFssSearchFailed(error);
                    throw new S100BuilderException("An error occurred while executing the ProductSearchNode.");
                }               

            } while (batchSearchResponse.Entries.Count < totalUpdateCount && !string.IsNullOrWhiteSpace(filter));

            batchSearchResponse.Count = queryCount;
            return batchSearchResponse;
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int size = DefaultSplitSize)
        {
            for (var i = 0; i < masterList.Count; i += size)
            {
                yield return masterList.GetRange(i, Math.Min(size, masterList.Count - i));
            }
        }

        private string GenerateQueryForFss(List<SearchBatchProducts> products)
        {
            var queryBuilder = new StringBuilder();

            if (products == null || products.Count == 0)
            {
                return string.Empty;
            }

            queryBuilder.Append('(');
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                queryBuilder.Append('(')
                    .AppendFormat(ProductNameQueryClause ?? string.Empty, product.ProductName)
                    .AppendFormat(EditionNumberQueryClause ?? string.Empty, product.EditionNumber);

                if (product.UpdateNumbers != null && product.UpdateNumbers.Any())
                {
                    queryBuilder.Append("((");
                    queryBuilder.Append(string.Join(" or ", product.UpdateNumbers.Select(u => string.Format(UpdateNumberQueryClause ?? string.Empty, u))));
                    queryBuilder.Append("))");
                }
                queryBuilder.Append(i == products.Count - 1 ? ")" : ") or ");
            }
            queryBuilder.Append(')');           
            return (queryBuilder.ToString());
        }

        private static List<SearchBatchProducts> ChunkProductsByUpdateNumberLimit(IEnumerable<SearchBatchProducts> products)
        {
            return [.. products.SelectMany(product =>
                SplitList(product.UpdateNumbers.ToList(), UpdateNumberLimit)
                    .Select(updateNumbers => new SearchBatchProducts
                    {
                        ProductName = product.ProductName,
                        EditionNumber = product.EditionNumber,
                        UpdateNumbers = updateNumbers
                    }))];
        }

        private IEnumerable<List<SearchBatchProducts>> ChunkProductsByProductLimit(IEnumerable<SearchBatchProducts> products)
        {
            return SplitList((ChunkProductsByUpdateNumberLimit(products)), ProductLimit);
        }

        private async Task CheckProductOrCancellationData(BatchDetails item)
        {
            //var aioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();

            //if (cancellationToken.IsCancellationRequested)
            //{ 
            //    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
            //    throw new OperationCanceledException();
            //}
            //if (CheckProductDoesExistInResponseItem(item, productItem) && productItem.Cancellation != null && productItem.Cancellation.UpdateNumber.HasValue
            //                        && Convert.ToInt32(updateNumber) == productItem.Cancellation.UpdateNumber.Value)
            //{
            //    await CheckProductWithCancellationData(internalSearchBatchResponse, productList, item, productItem, compareProducts, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
            //}
            //else if (CheckProductDoesExistInResponseItem(item, productItem)
            //&& CheckEditionNumberDoesExistInResponseItem(item, productItem) && CheckUpdateNumberDoesExistInResponseItem(item, productItem))
            //{
            //    internalSearchBatchResponse.Entries.Add(item);
            //    productList.Add(compareProducts);
                await DownloadEncFilesFromFssBatch(item);
            //}
        }

        private async Task DownloadEncFilesFromFssBatch(BatchDetails item)
        {
           await PerformBatchFileDownload(item);
                
        }

        private async Task PerformBatchFileDownload(BatchDetails item)
        {
            foreach (var file in item.Files)
            {
                var fileName = file.Links.Get.Href.Split("/").Last();
                //if (cancellationToken.IsCancellationRequested)
                //{
                //    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{Uri} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, queueMessage.BatchId, queueMessage.CorrelationId);
                //    throw new OperationCanceledException();
                //}

                var httpResponse = await _fileShareReadOnlyClient.DownloadFileAsync(item.BatchId, fileName);
                //var downloadPath = @"/usr/local/tomcat/ROOT/spool/fssdata";
                var downloadPath = @"CopyToFolder";

                if (!Directory.Exists(downloadPath))
                {
                    Directory.CreateDirectory(downloadPath);
                }

                var path = Path.Combine(downloadPath, fileName);
                httpResponse.IsSuccess(out var value, out var error);
                if (value != null)
                {
                    await using (var outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                    {
                        value.CopyTo(outputFileStream);
                    }
                }

            }
            //var productName = productItem.ProductName;
            //var editionNumber = Convert.ToString(productItem.EditionNumber);
            //var updateNumber = item.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();
            //return logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadENCFilesRequestStart, EventIds.DownloadENCFilesRequestCompleted,
            //    "File share service download request for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
            //    async () =>
            //    {
            //        var downloadPath = Path.Combine(exchangeSetRootPath, productName.Substring(0, 2), productName, editionNumber, updateNumber);
            //        return await fileShareDownloadService.DownloadBatchFiles(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
            //    }, productName, editionNumber, updateNumber, item.Files.Select(a => a.Links.Get.Href), message.BatchId, message.CorrelationId);
        }

        static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var queryIndex = queryString.IndexOf('?');
            var queryPart = queryIndex >= 0 ? queryString.Substring(queryIndex + 1) : queryString;
            var queryParams = HttpUtility.ParseQueryString(queryPart);
            var result = new Dictionary<string, string>();
            foreach (string key in queryParams)
            {
                if (key != null)
                {
                    result[key] = queryParams[key];
                }
            }

            return result;
        }
    }
}

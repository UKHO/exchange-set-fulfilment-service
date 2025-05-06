using System.Text;
using System.Web;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class ProductSearchNode : ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private const string ProductName = "$batch(ProductName) eq '{0}' and ";
        private const string EditionNumber = "$batch(EditionNumber) eq '{0}' and ";
        private const string UpdateNumber = "$batch(UpdateNumber) eq '{0}' ";
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductType = "$batch(ProductType) eq 'S-100' and ";
        private const int ParallelSearchTaskCount = 1;
        private const int UpdateNumberLimit = 5;
        private const int ProductLimit = 4;
        private const int Limit = 100;
        private const int Start = 0;
        private ILogger _logger;

        public ProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));

        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<AssemblyPipeline>();

            var products = context.Subject.Job?.Products;
            string correlationId = context.Subject.Job?.CorrelationId;
            if (products == null || products.Count == 0)
                return NodeResultStatus.Succeeded;

            var cancellationTokenSource = new CancellationTokenSource();
            var batchList = new List<BatchDetails>();

            var groupedProducts = products
                .GroupBy(p => p.ProductName)
                .Select(g => new SearchBatchProducts
                {
                    ProductName = g.Key,
                    EditionNumber = g.First().LatestEditionNumber,
                    UpdateNumbers = g.Select(p => p.LatestUpdateNumber).ToList()
                }).ToList();

            var productGroupCount = (int)Math.Ceiling((double)products.Count / ParallelSearchTaskCount);
            var productsList = SplitList(groupedProducts, productGroupCount);

            var tasks = productsList.Select(async productGroup =>
            {
                var batchDetails = await QueryFileShareServiceFilesAsync(productGroup, cancellationTokenSource, cancellationTokenSource.Token, correlationId);
                batchList.AddRange(batchDetails);
            });            
            await Task.WhenAll(tasks);
            _logger.LogProductSearchPipelineCompleted(batchList.Count);
            return NodeResultStatus.Succeeded;
        }

        private async Task<List<BatchDetails>> QueryFileShareServiceFilesAsync(List<SearchBatchProducts> products, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string correlationId)
        {
            var batchDetails = new List<BatchDetails>();
            if (products == null || products.Count == 0)
                return batchDetails;

            var batchProducts = SliceProductsForFssQuery(products);
            foreach (var productBatch in batchProducts)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //needs to handle code for cancellation token
                    //var productDetail = new StringBuilder();
                    //    var productDetail = new StringBuilder();
                    //    foreach (var productitem in item) 
                    //    {
                    //        productDetail.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1} and UpdateNumbers:[{2}]", productitem.ProductName, productitem.EditionNumber.ToString(), string.Join(",", productitem?.UpdateNumbers.Select(a => a.Value.ToString())));
                    //    }
                    //    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from File Share Service with cancellationToken:{cancellationTokenSource.Token} at time:{DateTime.UtcNow} and productdetails:{productDetail.ToString()} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), DateTime.UtcNow, productDetail.ToString(), message.BatchId, message.CorrelationId);
                    //throw new OperationCanceledException();
                    _logger.LogProductSearchPipelineFailed(correlationId);
                }

                var result = await FetchBatchDetailsForProductsAsync(productBatch, cancellationTokenSource, cancellationToken, correlationId);
                batchDetails.AddRange(result.Entries);
            }

            return batchDetails;
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int size = 30)
        {
            for (var i = 0; i < masterList.Count; i += size)
            {
                yield return masterList.GetRange(i, Math.Min(size, masterList.Count - i));
            }
        }

        private static (string, string) GenerateQueryForFss(List<SearchBatchProducts> products)
        {
            var queryBuilder = new StringBuilder();
            var logBuilder = new StringBuilder();

            if (products == null || products.Count == 0)
                return (string.Empty, string.Empty);

            queryBuilder.Append('(');
            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                queryBuilder.Append('(')
                    .AppendFormat(ProductName, product.ProductName)
                    .AppendFormat(EditionNumber, product.EditionNumber);

                if (product.UpdateNumbers != null && product.UpdateNumbers.Any())
                {
                    queryBuilder.Append("((");
                    queryBuilder.Append(string.Join(" or ", product.UpdateNumbers.Select(u => string.Format(UpdateNumber, u))));
                    queryBuilder.Append("))");
                }
                queryBuilder.Append(i == products.Count - 1 ? ")" : ") or ");
                logBuilder.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1}, UpdateNumbers:[{2}]",
                    product.ProductName, product.EditionNumber, string.Join(",", product.UpdateNumbers));

            }

            queryBuilder.Append(')');

            return (queryBuilder.ToString(), logBuilder.ToString());
        }

        private static List<SearchBatchProducts> SliceProductsWithUpdateNumberForFssQuery(List<SearchBatchProducts> products)
        {
            return [.. products.SelectMany(product =>
                SplitList(product.UpdateNumbers, UpdateNumberLimit)
                    .Select(updateNumbers => new SearchBatchProducts
                    {
                        ProductName = product.ProductName,
                        EditionNumber = product.EditionNumber,
                        UpdateNumbers = updateNumbers
                    }))];
        }

        private static IEnumerable<List<SearchBatchProducts>> SliceProductsForFssQuery(List<SearchBatchProducts> products)
        {
            return SplitList((SliceProductsWithUpdateNumberForFssQuery(products)), ProductLimit);
        }

        private async Task<BatchSearchResponse> FetchBatchDetailsForProductsAsync(List<SearchBatchProducts> products,
            CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string correlationId)
        {
            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };
            if (products == null || products.Count == 0)
                return batchSearchResponse;

            var productQuery = GenerateQueryForFss(products);

            var totalUpdateCount = products.Sum(p => p.UpdateNumbers.Count);
            var queryCount = 0;
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductType} {productQuery.Item1}";
            var limit = Limit;
            var start = Start;
            HttpResponseMessage httpResponse;
            do
            {
                queryCount++;
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogProductSearchPipelineFailed(correlationId);
                    //To do -add log message
                    //logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files with cancellationToken:{cancellationTokenSource.Token} and uri:{Uri} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, message.BatchId, message.CorrelationId);
                    //throw new OperationCanceledException();
                }
                var result = await _fileShareReadOnlyClient.SearchAsync(filter, limit, start,correlationId, cancellationToken);
                if (result.IsSuccess(out var value, out _))
                {
                    if (value.Entries.Count != 0)
                    {
                        batchSearchResponse.Entries.AddRange(value.Entries);
                    }
                }
                else
                {
                    cancellationTokenSource.Cancel();
                    _logger.LogProductSearchPipelineFailed(correlationId);
                    break;
                    //To do -add log message
                    //        //logger.LogError(EventIds.QueryFileShareServiceENCFilesNonOkResponse.ToEventId(), "Error in file share service while searching ENC files with uri:{RequestUri}, responded with {StatusCode} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, message.BatchId, message.CorrelationId);
                    //        //logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in file share service while searching ENC files with cancellationToken:{cancellationTokenSource.Token} with uri:{RequestUri}, responded with {StatusCode} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, message.BatchId, message.CorrelationId);
                    //        //throw new FulfilmentException(EventIds.QueryFileShareServiceENCFilesNonOkResponse.ToEventId());
                }


                var queryString = batchSearchResponse.Links?.Next?.Href;
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

            } while (batchSearchResponse.Entries.Count < totalUpdateCount && !string.IsNullOrWhiteSpace(filter));

            batchSearchResponse.Count = queryCount;
            return batchSearchResponse;
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

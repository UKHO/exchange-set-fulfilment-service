using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.EFS.Configuration.Builder;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class ProductSearchNode : ExchangeSetPipelineNode
    {
        private readonly FileShareServiceSettings _fileShareServiceSettings;
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;

        public ProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient, IOptions<FileShareServiceSettings> options)
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
            _fileShareServiceSettings = options.Value;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<AssemblyPipeline>();

            var products = context.Subject.Job?.Products;
            var jobId = context.Subject.Job?.Id;
            var correlationId = context.Subject.Job?.CorrelationId;
            if (products == null || products.Count == 0)
                return NodeResultStatus.NotRun;

            var batchList = new List<BatchDetails>();

            var groupedProducts = products
                .GroupBy(p => p.ProductName)
                .Select(g => new SearchBatchProducts
                {
                    ProductName = g.Key,
                    EditionNumber = g.First().LatestEditionNumber,
                    UpdateNumbers = g.Select(p => p.LatestUpdateNumber).ToList()
                }).ToList();

            var productGroupCount = (int)Math.Ceiling((double)products.Count / _fileShareServiceSettings.ParallelSearchTaskCount);
            var productsList = SplitList(groupedProducts, productGroupCount);

            var tasks = productsList.Select(async productGroup =>
            {
                var batchDetails = await QueryFileShareServiceFilesAsync(productGroup, correlationId, jobId);
                if (batchDetails != null)
                    batchList.AddRange(batchDetails);
            });
            await Task.WhenAll(tasks);
            context.Subject.BatchDetails = batchList;
            return NodeResultStatus.Succeeded;
        }

        private async Task<List<BatchDetails>> QueryFileShareServiceFilesAsync(List<SearchBatchProducts> products, string correlationId, string jobId)
        {
            var batchDetails = new List<BatchDetails>();
            if (products == null || products.Count == 0)
                return batchDetails;

            var batchProducts = SliceProductsForFssQuery(products);
            foreach (var productBatch in batchProducts)
            {
                var result = await FetchBatchDetailsForProductsAsync(productBatch, correlationId, jobId);
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

        private (string, string) GenerateQueryForFss(List<SearchBatchProducts> products)
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
                    .AppendFormat(_fileShareServiceSettings.ProductName, product.ProductName)
                    .AppendFormat(_fileShareServiceSettings.EditionNumber, product.EditionNumber);

                if (product.UpdateNumbers != null && product.UpdateNumbers.Any())
                {
                    queryBuilder.Append("((");
                    queryBuilder.Append(string.Join(" or ", product.UpdateNumbers.Select(u => string.Format(_fileShareServiceSettings.UpdateNumber, u))));
                    queryBuilder.Append("))");
                }
                queryBuilder.Append(i == products.Count - 1 ? ")" : ") or ");
                logBuilder.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1}, UpdateNumbers:[{2}]",
                    product.ProductName, product.EditionNumber, string.Join(",", product.UpdateNumbers));
            }

            queryBuilder.Append(')');

            return (queryBuilder.ToString(), logBuilder.ToString());
        }

        private List<SearchBatchProducts> SliceProductsWithUpdateNumberForFssQuery(List<SearchBatchProducts> products)
        {
            return [.. products.SelectMany(product =>
                SplitList(product.UpdateNumbers, _fileShareServiceSettings.UpdateNumberLimit)
                    .Select(updateNumbers => new SearchBatchProducts
                    {
                        ProductName = product.ProductName,
                        EditionNumber = product.EditionNumber,
                        UpdateNumbers = updateNumbers
                    }))];
        }

        private IEnumerable<List<SearchBatchProducts>> SliceProductsForFssQuery(List<SearchBatchProducts> products)
        {
            return SplitList((SliceProductsWithUpdateNumberForFssQuery(products)), _fileShareServiceSettings.ProductLimit);
        }

        private async Task<BatchSearchResponse> FetchBatchDetailsForProductsAsync(List<SearchBatchProducts> products,
             string correlationId, string jobId)
        {
            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };
            if (products == null || products.Count == 0)
                return batchSearchResponse;

            var productQuery = GenerateQueryForFss(products);

            var totalUpdateCount = products.Sum(p => p.UpdateNumbers.Count);
            var queryCount = 0;
            var filter = $"BusinessUnit eq '{_fileShareServiceSettings.BusinessUnit}' and {_fileShareServiceSettings.ProductType} {productQuery.Item1}";
            var limit = _fileShareServiceSettings.Limit;
            var start = _fileShareServiceSettings.Start;
            do
            {
                queryCount++;
                var result = await _fileShareReadOnlyClient.SearchAsync(filter, limit, start, correlationId);
                if (result.IsSuccess(out var value, out var error))
                {
                    if (value.Entries.Count != 0)
                    {
                        batchSearchResponse.Entries.AddRange(value.Entries);
                    }
                }
                else
                {
                    _logger.LogProductSearchNodeFailed(jobId, error);
                    break;
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

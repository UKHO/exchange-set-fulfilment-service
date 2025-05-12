using System.Text;
using System.Web;
using Microsoft.Extensions.Options;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.EFS.Configuration.Builder;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    public class ProductSearchNode : ExchangeSetPipelineNode
    {
        private readonly IOptions<FileShareServiceConfiguration> _fileShareServiceConfiguration;
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;
        private const int DefaultSplitSize = 30;

        public ProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient, IOptions<FileShareServiceConfiguration> fileShareServiceOptions) : base()
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
            _fileShareServiceConfiguration = fileShareServiceOptions ?? throw new ArgumentNullException(nameof(fileShareServiceOptions));
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
                else
                {
                    var batchList = new List<BatchDetails>();
                    var groupedProducts = products
                        .GroupBy(p => p.ProductName)
                        .Select(g => new SearchBatchProducts
                        {
                            ProductName = g.Key,
                            EditionNumber = g.First().LatestEditionNumber,
                            UpdateNumbers = g.Select(p => p.LatestUpdateNumber).ToList()
                        }).ToList();

                    var productGroupCount = (int)Math.Ceiling((double)products.Count / _fileShareServiceConfiguration.Value.ParallelSearchTaskCount);
                    var productsList = SplitList(groupedProducts, productGroupCount);
                    foreach (var productGroup in productsList)
                    {
                        var batchDetails = await QueryFileShareServiceFilesAsync(productGroup, context.Subject.Job?.CorrelationId);
                        if (batchDetails != null)
                            batchList.AddRange(batchDetails);
                    }

                    context.Subject.BatchDetails = batchList;
                    return NodeResultStatus.Succeeded;
                }
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
            else
            {
                var batchProducts = ChunkProductsByProductLimit(products);
                foreach (var productBatch in batchProducts)
                {
                    var result = await FetchBatchDetailsForProductsAsync(productBatch, correlationId);
                    batchDetails.AddRange(result.Entries);
                }

                return batchDetails;
            }
        }

        private async Task<BatchSearchResponse> FetchBatchDetailsForProductsAsync(List<SearchBatchProducts> products,
             string correlationId)
        {
            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };
            if (products == null || products.Count == 0)
            {
                return batchSearchResponse;
            }
            else
            {
                var productQuery = GenerateQueryForFss(products);
                var totalUpdateCount = products.Sum(p => p.UpdateNumbers.Count);
                var queryCount = 0;
                var filter = $"BusinessUnit eq '{_fileShareServiceConfiguration.Value.BusinessUnit}' and {_fileShareServiceConfiguration.Value.ProductType} {productQuery}";
                var limit = _fileShareServiceConfiguration.Value.Limit;
                var start = _fileShareServiceConfiguration.Value.Start;
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
                        _logger.LogProductSearchNodeFssSearchFailed(error);
                        throw new S100BuilderException("An error occurred while executing the ProductSearchNode.");
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
            var logBuilder = new StringBuilder();

            if (products == null || products.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                queryBuilder.Append('(');
                for (var i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    queryBuilder.Append('(')
                        .AppendFormat(_fileShareServiceConfiguration.Value.ProductName ?? string.Empty, product.ProductName)
                        .AppendFormat(_fileShareServiceConfiguration.Value.EditionNumber ?? string.Empty, product.EditionNumber);

                    if (product.UpdateNumbers != null && product.UpdateNumbers.Any())
                    {
                        queryBuilder.Append("((");
                        queryBuilder.Append(string.Join(" or ", product.UpdateNumbers.Select(u => string.Format(_fileShareServiceConfiguration.Value.UpdateNumber ?? string.Empty, u))));
                        queryBuilder.Append("))");
                    }
                    queryBuilder.Append(i == products.Count - 1 ? ")" : ") or ");
                    logBuilder.AppendFormat("\n Product/ProductName:{0}, EditionNumber:{1}, UpdateNumbers:[{2}]",
                        product.ProductName,
                        product.EditionNumber,
                        string.Join(",", product?.UpdateNumbers?.Where(u => u.HasValue) ?? []));
                }
                queryBuilder.Append(')');
                _logger.LogProductSearchNodeFssQueryProducts(logBuilder.ToString());
                return (queryBuilder.ToString());
            }
        }

        private List<SearchBatchProducts> ChunkProductsByUpdateNumberLimit(List<SearchBatchProducts> products)
        {
            return [.. products.SelectMany(product =>
                SplitList(product.UpdateNumbers, _fileShareServiceConfiguration.Value.UpdateNumberLimit)
                    .Select(updateNumbers => new SearchBatchProducts
                    {
                        ProductName = product.ProductName,
                        EditionNumber = product.EditionNumber,
                        UpdateNumbers = updateNumbers
                    }))];
        }

        private IEnumerable<List<SearchBatchProducts>> ChunkProductsByProductLimit(List<SearchBatchProducts> products)
        {
            return SplitList((ChunkProductsByUpdateNumberLimit(products)), _fileShareServiceConfiguration.Value.ProductLimit);
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

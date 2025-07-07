using System.Text;
using System.Web;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Models;
using UKHO.ADDS.EFS.Exceptions;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble
{
    internal class ProductSearchNode : S100ExchangeSetPipelineNode
    {
        private readonly IFileShareReadOnlyClient _fileShareReadOnlyClient;
        private ILogger _logger;
        private const int DefaultSplitSize = 30;

        private const string ProductNameQueryClause = "$batch(ProductName) eq '{0}' and ";
        private const string EditionNumberQueryClause = "$batch(EditionNumber) eq '{0}' and ";
        private const string UpdateNumberQueryClause = "$batch(UpdateNumber) eq '{0}' ";
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductType = "S-100";
        private const string ProductTypeQueryClause = $"$batch(ProductType) eq '{ProductType}' and ";
        private const int MaxSearchOperations = 5;
        private const int UpdateNumberLimit = 5;
        private const int ProductLimit = 4;
        private const int Limit = 100;
        private const int Start = 0;
        private const string QueryLimit = "limit";
        private const string QueryStart = "start";
        private const string QueryFilter = "$filter";


        public ProductSearchNode(IFileShareReadOnlyClient fileShareReadOnlyClient) : base()
        {
            _fileShareReadOnlyClient = fileShareReadOnlyClient ?? throw new ArgumentNullException(nameof(fileShareReadOnlyClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetPipelineContext> context)
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
                    .Select(g => new BatchProductDetail
                    {
                        ProductName = g.Key,
                        EditionNumber = g.First().LatestEditionNumber,
                        UpdateNumbers = g.Select(p => p.LatestUpdateNumber).ToList()
                    }).ToList();

                var productGroupCount = (int)Math.Ceiling((double)products.Count / MaxSearchOperations);
                var productsList = SplitList(groupedProducts, productGroupCount);

                foreach (var productGroup in productsList)
                {
                    var batchDetails = await QueryFileShareServiceFilesAsync(productGroup, context.Subject.Job?.GetCorrelationId()!);
                    if (batchDetails != null)
                    {
                        batchList.AddRange(batchDetails);
                    }
                }
                context.Subject.BatchDetails = batchList;
                return NodeResultStatus.Succeeded;
            }
            catch (S100BuilderException)
            {
                return NodeResultStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogProductSearchNodeFailed(ex);
                return NodeResultStatus.Failed;
            }
        }

        private async Task<List<BatchDetails>> QueryFileShareServiceFilesAsync(List<BatchProductDetail> products, string correlationId)
        {
            var batchDetails = new List<BatchDetails>();

            var batchProducts = ChunkProductsByProductLimit(products);
            foreach (var productBatch in batchProducts)
            {
                var result = await FetchBatchDetailsForProductsAsync(productBatch, correlationId);
                batchDetails.AddRange(result.Entries);
            }

            return batchDetails;
        }

        private async Task<BatchSearchResponse> FetchBatchDetailsForProductsAsync(List<BatchProductDetail> products,
             string correlationId)
        {
            var batchSearchResponse = new BatchSearchResponse { Entries = new List<BatchDetails>() };

            var productQuery = GenerateQueryForFss(products);
            var totalUpdateCount = products.Sum(p => p.UpdateNumbers.ToList().Count);
            var queryCount = 0;
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductTypeQueryClause}{productQuery}";
            var limit = Limit;
            var start = Start;
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<BatchSearchResponse>(_logger, "SearchAsync");
            do
            {
                queryCount++;
                var result = await retryPolicy.ExecuteAsync(() =>
                    _fileShareReadOnlyClient.SearchAsync(filter, limit, start, correlationId));
                if (result.IsSuccess(out var value, out var error))
                {
                    if (value.Entries.Count != 0)
                    {
                        batchSearchResponse.Entries.AddRange(value.Entries);
                    }

                    var queryString = value.Links?.Next?.Href;
                    if (!string.IsNullOrEmpty(queryString))
                    {
                        var parsedValues = ParseQueryString(queryString);
                        limit = parsedValues.TryGetValue(QueryLimit, out var urlLimit) ? int.Parse(urlLimit) : limit;
                        start = parsedValues.TryGetValue(QueryStart, out var urlStart) ? int.Parse(urlStart) : start;
                        filter = parsedValues.TryGetValue(QueryFilter, out var urlFilter) ? urlFilter : filter;
                    }
                    else
                    {
                        filter = string.Empty;
                    }
                }
                else
                {
                    LogFssSearchFailed(products, correlationId, filter, limit, start, error);

                    throw new S100BuilderException("An error occurred while executing the ProductSearchNode.");
                }

            } while (batchSearchResponse.Entries.Count < totalUpdateCount && !string.IsNullOrWhiteSpace(filter));

            batchSearchResponse.Count = queryCount;
            return batchSearchResponse;
        }

        private void LogFssSearchFailed(IEnumerable<BatchProductDetail> products, string correlationId, string filter, int limit, int start, IError error)
        {
            var searchQuery = new SearchQuery
            {
                Filter = filter,
                Limit = limit,
                Start = start
            };
            var batchSearchProductsLogView = new BatchProductSearchLog
            {
                BatchProducts = products,
                CorrelationId = correlationId,
                BusinessUnit = BusinessUnit,
                ProductType = ProductType,
                Query = searchQuery,
                Error = error
            };

            _logger.LogProductSearchNodeFssSearchFailed(batchSearchProductsLogView);
        }

        private static IEnumerable<List<BatchProductDetail>> ChunkProductsByProductLimit(IEnumerable<BatchProductDetail> products)
        {
            return SplitList((ChunkProductsByUpdateNumberLimit(products)), ProductLimit);
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int size = DefaultSplitSize)
        {
            for (var i = 0; i < masterList.Count; i += size)
            {
                yield return masterList.GetRange(i, Math.Min(size, masterList.Count - i));
            }
        }

        private static string GenerateQueryForFss(List<BatchProductDetail> products)
        {
            var queryBuilder = new StringBuilder();

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

        private static List<BatchProductDetail> ChunkProductsByUpdateNumberLimit(IEnumerable<BatchProductDetail> products)
        {
            return [.. products.SelectMany(product =>
                SplitList(product.UpdateNumbers.ToList(), UpdateNumberLimit)
                    .Select(updateNumbers => new BatchProductDetail
                    {
                        ProductName = product.ProductName,
                        EditionNumber = product.EditionNumber,
                        UpdateNumbers = updateNumbers
                    }))];
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
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

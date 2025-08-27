using System.Net;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Products;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;
using ProductCounts = UKHO.ADDS.EFS.Products.ProductCountSummary;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    /// <summary>
    ///     Service responsible for retrieving product information from the Sales Catalogue.
    /// </summary>
    internal class OrchestratorSalesCatalogueClient : IOrchestratorSalesCatalogueClient
    {
        private readonly ILogger<OrchestratorSalesCatalogueClient> _logger;
        private readonly KiotaSalesCatalogueService _salesCatalogueClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrchestratorSalesCatalogueClient" /> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public OrchestratorSalesCatalogueClient(KiotaSalesCatalogueService salesCatalogueClient, ILogger<OrchestratorSalesCatalogueClient> logger)
        {
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Retrieves S100 products that have been modified since a specific date.
        /// </summary>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="job">The build.</param>
        /// <returns>
        ///     A tuple containing:
        ///     - s100SalesCatalogueData: The response from the Sales Catalogue API.
        ///     - LastModified: The timestamp when the data was last modified. Will be the original sinceDateTime if response is
        ///     NotModified.
        /// </returns>
        /// <remarks>
        ///     The method returns an empty response with the original sinceDateTime when an error occurs or when
        ///     an unexpected HTTP status code is returned from the API.
        /// </remarks>
        public async Task<(ProductVersionList s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, Job job)
        {
            var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
            try
            {
                var headerDateString = sinceDateTime?.ToString("R");
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                  {
                      config.Headers.Add("If-Modified-Since", headerDateString!);
                      config.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                      config.Options.Add(headersOption);
                  });
                    return Result.Success(result);
                });

                var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModified, out var lastModifiedActual);

                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList) && catalogueList != null)
                {
                    var response = new ProductVersionList
                    {
                        ResponseBody = [.. catalogueList.Select(x =>
                        {
                            ProductStatus? parsedStatus = null;

                            if (x.Status?.StatusDate != null)
                            {
                                parsedStatus = new ProductStatus() { StatusDate = x.Status.StatusDate.Value.DateTime, StatusName = x.Status.StatusName.ToString() };
                            }
                            return new ProductVersion
                            {
                                ProductName = ProductName.From(x.ProductName!),
                                LatestEditionNumber = x.LatestEditionNumber.HasValue ? EditionNumber.From(x.LatestEditionNumber!.Value) : EditionNumber.NotSet,
                                LatestUpdateNumber = x.LatestUpdateNumber.HasValue ? UpdateNumber.From(x.LatestUpdateNumber!.Value) : UpdateNumber.NotSet,
                                Status = parsedStatus
                            };
                        })],
                        LastModified = lastModifiedActual,
                        ResponseCode = HttpStatusCode.OK
                    };
                    return (response, response.LastModified);
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));

                return (new ProductVersionList(), sinceDateTime);
            }

            catch (ApiException apiException)
            {
                switch (apiException.ResponseStatusCode)
                {

                    case (int)HttpStatusCode.NotModified:
                        var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                            ? values.FirstOrDefault()
                            : null;
                        return (new ProductVersionList() { ResponseCode = HttpStatusCode.NotModified }, DateTime.Parse(lastModified!));
                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return (new ProductVersionList(), sinceDateTime);
                }
            }
        }

        /// <summary>
        ///     Retrieves S100 product names and their details from the Sales Catalogue Service.
        /// </summary>
        /// <param name="productNames">A collection of product names to retrieve.</param>
        /// <param name="job">The job context for the request.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The response containing product details or an empty response if an error occurs.
        /// </returns>
        /// <remarks>
        ///     The method returns an empty response when an error occurs or when
        ///     an unexpected HTTP status code is returned from the API.
        /// </remarks>
        public async Task<ProductEditionList> GetS100ProductNamesAsync(IEnumerable<ProductName> productNames, Job job, CancellationToken cancellationToken)
        {
            try
            {
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetS100ProductNamesAsync));
                var S100ProductNamesResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Products.S100.ProductNames.PostAsync(productNames.Select(x => (string)x).ToList(), requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());

                }, cancellationToken);
                    return Result.Success(result);
                });

                if (S100ProductNamesResult.IsSuccess(out var response) && response != null)
                {
                    return new ProductEditionList
                    {
                        Products = response.Products?.Select(x => new ProductEdition
                        {
                            ProductName = ProductName.From(x.ProductName!),
                            EditionNumber = x.EditionNumber.HasValue ? EditionNumber.From(x.EditionNumber!.Value) : EditionNumber.NotSet,
                            UpdateNumbers = x.UpdateNumbers != null ? x.UpdateNumbers.Where(i => i.HasValue).Select(i => i.Value).ToList() : new List<int>(),
                            Dates = x.Dates?.Select(d => new ProductDate
                            {
                                IssueDate = d.IssueDate?.DateTime ?? default,
                                UpdateApplicationDate = d.UpdateApplicationDate?.DateTime ?? default,
                                UpdateNumber = d.UpdateNumber.HasValue ? UpdateNumber.From(d.UpdateNumber!.Value) : UpdateNumber.NotSet
                            }).ToList() ?? new List<ProductDate>(),
                            FileSize = x.FileSize ?? 0,
                            Cancellation = x.Cancellation is null
                                    ? null
                                    : new ProductCancellation
                                    {
                                        EditionNumber = x.Cancellation!.EditionNumber.HasValue ? EditionNumber.From(x.Cancellation!.EditionNumber.Value) : EditionNumber.NotSet,
                                        UpdateNumber = x.Cancellation!.UpdateNumber.HasValue ? UpdateNumber.From(x.Cancellation!.UpdateNumber.Value) : UpdateNumber.NotSet
                                    }

                        }).ToList() ?? new List<ProductEdition>(),
                        ProductCountSummary = response?.ProductCounts is null ? null : new ProductCounts
                        {
                            RequestedProductCount = response.ProductCounts.RequestedProductCount.HasValue ? ProductCount.From(response.ProductCounts.RequestedProductCount!.Value) : ProductCount.None,
                            ReturnedProductCount = response.ProductCounts.ReturnedProductCount.HasValue ? ProductCount.From(response.ProductCounts.ReturnedProductCount!.Value) : ProductCount.None ,
                            RequestedProductsAlreadyUpToDateCount = response.ProductCounts.RequestedProductsAlreadyUpToDateCount.HasValue ? ProductCount.From(response.ProductCounts.RequestedProductsAlreadyUpToDateCount!.Value) : ProductCount.None,
                            MissingProducts = response.ProductCounts.RequestedProductsNotReturned?.Select(r => new MissingProduct
                            {
                                ProductName = ProductName.From(r.ProductName!),
                                Reason = r.Reason?.ToString() ?? string.Empty
                            }).ToList() ?? new List<MissingProduct>()
                        },
                        ResponseCode = HttpStatusCode.OK
                    };
                }
                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return new ProductEditionList();
            }
            catch (ApiException apiException)
            {
                _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                return new ProductEditionList();
            }
        }

    }
}

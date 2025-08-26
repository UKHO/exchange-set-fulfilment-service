using System.Net;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    /// <summary>
    ///     Service responsible for retrieving product information from the Sales Catalogue.
    /// </summary>
    internal class OrchestratorSalesCatalogueClient : IOrchestratorSalesCatalogueClient
    {
        private readonly ILogger<OrchestratorSalesCatalogueClient> _logger;
        private readonly ISalesCatalogueKiotaClientAdapter _salesCatalogueKiotaClientAdapter;
        private readonly IConfiguration _configuration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrchestratorSalesCatalogueClient" /> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        /// <param name="configuration">Configuration for accessing settings.</param>
        public OrchestratorSalesCatalogueClient(ISalesCatalogueKiotaClientAdapter salesCatalogueKiotaClientAdapter, ILogger<OrchestratorSalesCatalogueClient> logger, IConfiguration configuration)
        {
            _salesCatalogueKiotaClientAdapter = salesCatalogueKiotaClientAdapter ?? throw new ArgumentNullException(nameof(salesCatalogueKiotaClientAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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
        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, Job job)
        {
            // Check if we should skip SCS and use mock data for FSS testing
            var skipScs = _configuration.GetValue<bool>("orchestrator:SkipScsForTesting", false) ||
                         Environment.GetEnvironmentVariable("SKIP_SCS_FOR_TESTING")?.ToLowerInvariant() == "true";

            if (skipScs)
            {
               // _logger.LogWarning("TESTING MODE: Skipping SCS call and returning mock data for FSS testing. Job={JobId}", job.Id);
                return GetMockS100ProductsResponse();
            }

            var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
            try
            {
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueKiotaClientAdapter.GetBasicCatalogueAsync(sinceDateTime, job, headersOption, CancellationToken.None);
                    return Result.Success(result);
                });

                var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModified, out var lastModifiedActual);

                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList) && catalogueList != null)
                {
                    var response = new S100SalesCatalogueResponse
                    {
                        ResponseBody = [.. catalogueList.Select(x =>
                        {
                            S100ProductStatus? parsedStatus = null;
                            if (x.Status?.StatusDate != null)
                            {
                                parsedStatus = new S100ProductStatus() { StatusDate = x.Status.StatusDate.Value.DateTime, StatusName = x.Status.StatusName.ToString() };
                            }
                            return new S100Products
                            {
                                ProductName = x.ProductName,
                                LatestEditionNumber = x.LatestEditionNumber,
                                LatestUpdateNumber = x.LatestUpdateNumber,
                                Status = parsedStatus
                            };
                        })],
                        LastModified = lastModifiedActual,
                        ResponseCode = HttpStatusCode.OK
                    };
                    return (response, response.LastModified);
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }
            catch (ApiException apiException)
            {
                switch (apiException.ResponseStatusCode)
                {
                    case (int)HttpStatusCode.NotModified:
                        var lastModifiedDateHeader = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                            ? values.FirstOrDefault()
                            : null;
                        DateTime? lastModifiedDate = null;
                        if (!string.IsNullOrEmpty(lastModifiedDateHeader) && DateTime.TryParse(lastModifiedDateHeader, out var parsedDate))
                        {
                            lastModifiedDate = parsedDate;
                        }
                        return (new S100SalesCatalogueResponse() { ResponseCode = HttpStatusCode.NotModified }, lastModifiedDate);
                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return (new S100SalesCatalogueResponse() { ResponseCode = (HttpStatusCode)apiException.ResponseStatusCode }, sinceDateTime);
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
        public async Task<S100ProductNamesResponse> GetS100ProductNamesAsync(IEnumerable<string> productNames, Job job, CancellationToken cancellationToken)
        {
            // Check if we should skip SCS and use mock data for FSS testing
            var skipScs = _configuration.GetValue<bool>("orchestrator:SkipScsForTesting", false) ||
                         Environment.GetEnvironmentVariable("SKIP_SCS_FOR_TESTING")?.ToLowerInvariant() == "true";

            if (skipScs)
            {
                //_logger.LogWarning("TESTING MODE: Skipping SCS product names call and returning mock data for FSS testing. Job={JobId}", job.Id);
                return GetMockS100ProductNamesResponse(productNames);
            }

            try
            {
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetS100ProductNamesAsync));
                var S100ProductNamesResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueKiotaClientAdapter.PostProductNamesAsync(productNames.ToList(), job, cancellationToken);
                    return Result.Success(result);
                });

                if (S100ProductNamesResult.IsSuccess(out var response) && response != null)
                {
                    return new S100ProductNamesResponse
                    {
                        Products = response.Products?.Select(x => new S100ProductNames
                        {
                            ProductName = x.ProductName ?? string.Empty,
                            EditionNumber = x.EditionNumber ?? 0,
                            UpdateNumbers = x.UpdateNumbers != null ? x.UpdateNumbers.Where(i => i.HasValue).Select(i => i.Value).ToList() : new List<int>(),
                            Dates = x.Dates?.Select(d => new S100ProductDate
                            {
                                IssueDate = d.IssueDate.HasValue ? d.IssueDate.Value.DateTime : default,
                                UpdateApplicationDate = d.UpdateApplicationDate.HasValue ? d.UpdateApplicationDate.Value.DateTime : default,
                                UpdateNumber = d.UpdateNumber ?? 0
                            }).ToList() ?? new List<S100ProductDate>(),
                            FileSize = x.FileSize ?? 0,
                            Cancellation = x.Cancellation is null
                                ? null
                                : new S100ProductCancellation
                                {
                                    EditionNumber = x.Cancellation?.EditionNumber ?? 0,
                                    UpdateNumber = x.Cancellation?.UpdateNumber ?? 0
                                }
                        }).ToList() ?? new List<S100ProductNames>(),
                        ProductCounts = response?.ProductCounts is null ? null : new Clients.SalesCatalogueService.Models.ProductCounts
                        {
                            RequestedProductCount = response.ProductCounts.RequestedProductCount,
                            ReturnedProductCount = response.ProductCounts.ReturnedProductCount,
                            RequestedProductsAlreadyUpToDateCount = response.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                            RequestedProductsNotReturned = response.ProductCounts.RequestedProductsNotReturned?.Select(r => new RequestedProductsNotReturned
                            {
                                ProductName = r.ProductName ?? string.Empty,
                                Reason = r.Reason?.ToString() ?? string.Empty
                            }).ToList() ?? new List<RequestedProductsNotReturned>()
                        },
                        ResponseCode = HttpStatusCode.OK
                    };
                }
                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
            catch (ApiException apiException)
            {
                _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                return new S100ProductNamesResponse();
            }
        }

        /// <summary>
        /// Returns mock S100 products for FSS testing.
        /// </summary>
        private (S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified) GetMockS100ProductsResponse()
        {
            var mockProducts = new List<S100Products>
            {
                new()
                {
                    ProductName = "TEST_FSS_PRODUCT_001",
                    LatestEditionNumber = 1,
                    LatestUpdateNumber = 0,
                    Status = new S100ProductStatus
                    {
                        StatusDate = DateTime.UtcNow.AddDays(-1),
                        StatusName = "Base"
                    }
                },
                new()
                {
                    ProductName = "TEST_FSS_PRODUCT_002",
                    LatestEditionNumber = 2,
                    LatestUpdateNumber = 1,
                    Status = new S100ProductStatus
                    {
                        StatusDate = DateTime.UtcNow.AddDays(-2),
                        StatusName = "Update"
                    }
                }
            };

            var response = new S100SalesCatalogueResponse
            {
                ResponseBody = mockProducts,
                LastModified = DateTime.UtcNow,
                ResponseCode = HttpStatusCode.OK
            };

            return (response, response.LastModified);
        }

        /// <summary>
        /// Returns mock S100 product names for FSS testing.
        /// </summary>
        private S100ProductNamesResponse GetMockS100ProductNamesResponse(IEnumerable<string> productNames)
        {
            var mockProductDetails = productNames.Select(name => new S100ProductNames
            {
                ProductName = name,
                EditionNumber = 1,
                UpdateNumbers = [0],
                Dates = [new S100ProductDate
                {
                    IssueDate = DateTime.UtcNow.AddDays(-1),
                    UpdateApplicationDate = DateTime.UtcNow.AddDays(-1),
                    UpdateNumber = 0
                }],
                FileSize = 1024000,
                Cancellation = null
            }).ToList();

            return new S100ProductNamesResponse
            {
                Products = mockProductDetails,
                ProductCounts = new Clients.SalesCatalogueService.Models.ProductCounts
                {
                    RequestedProductCount = productNames.Count(),
                    ReturnedProductCount = mockProductDetails.Count,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = []
                },
                ResponseCode = HttpStatusCode.OK
            };
        }
    }
}

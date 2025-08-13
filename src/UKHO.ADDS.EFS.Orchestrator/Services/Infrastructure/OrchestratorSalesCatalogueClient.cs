using System.Net;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    /// <summary>
    ///     Service responsible for retrieving product information from the Sales Catalogue.
    /// </summary>
    internal class OrchestratorSalesCatalogueClient : IOrchestratorSalesCatalogueClient
    {
        private const string ScsApiVersion = "v2";
        private const string ProductType = "s100";
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
        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, Job job)
        {
            try
            {
                var headersOption = new HeadersInspectionHandlerOption { InspectResponseHeaders = true };
                var headerDateString = sinceDateTime?.ToString("R");

                var s100BasicCatalogueResult = await _salesCatalogueClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                {
                    config.Headers.Add("If-Modified-Since", headerDateString);
                    config.Headers.Add("X-Correlation-Id", job.GetCorrelationId());
                    config.Options.Add(headersOption);
                });

                var lastModified = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModified, out var lastModifiedActual);

                if (s100BasicCatalogueResult != null)
                {
                    var response = new S100SalesCatalogueResponse
                    {
                        ResponseBody = s100BasicCatalogueResult.Select(x =>
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
                        }).ToList(),
                        LastModified = lastModifiedActual,
                        ResponseCode = HttpStatusCode.OK
                    };

                    switch (response.ResponseCode)
                    {
                        case HttpStatusCode.OK:
                            return (response, response.LastModified);

                        case HttpStatusCode.NotModified:
                            return (response, response.LastModified);

                        default:
                            _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, response.ResponseCode));
                            return (new S100SalesCatalogueResponse(), sinceDateTime);
                    }
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));

                return (new S100SalesCatalogueResponse(), sinceDateTime);
            }

            catch (ApiException apiException)
            {
                switch (apiException.ResponseStatusCode)
                {
                    // TODO Last-Modified header is not returned in the response, so is this a problem with the ADDS Mock endpoint definition?
                    // Kiota should be able to parse the 304 in the Open API definition and return the body without throwing an exception,
                    // so this indicates a non-standard or deprecated definition in the spec YAML used to generate the client?

                    case (int)HttpStatusCode.NotModified:
                        var lastModified = apiException.ResponseHeaders.TryGetValue("Last-Modified", out var lastModifiedValues)
                            ? lastModifiedValues.FirstOrDefault() : null;

                        return (new S100SalesCatalogueResponse(), DateTime.Parse(lastModified!));
                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return (new S100SalesCatalogueResponse(), sinceDateTime);
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
            try
            {
                var response = await _salesCatalogueClient.V2.Products.S100.ProductNames.PostAsync(productNames.ToList(), requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("X-Correlation-Id", job.GetCorrelationId());

                }, cancellationToken);

                return new S100ProductNamesResponse
                {
                    Products = response?.Products?.Select(x => new S100ProductNames
                    {
                        ProductName = x.ProductName,
                        EditionNumber = x.EditionNumber ?? 0,
                        UpdateNumbers = x.UpdateNumbers != null ? x.UpdateNumbers.Where(i => i.HasValue).Select(i => i.Value).ToList() : new List<int>(),
                        Dates = x.Dates?.Select(d => new S100ProductDate
                        {
                            // Map properties as needed

                        }).ToList() ?? new List<S100ProductDate>(),

                        FileSize = x.FileSize ?? 0,

                        Cancellation = x.Cancellation is null ? null : new S100ProductCancellation
                        {
                            // Map properties as needed
                        }

                    }).ToList() ?? new List<S100ProductNames>(),

                    ProductCounts = response?.ProductCounts is null ? null : new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts
                    {
                        RequestedProductCount = response.ProductCounts.RequestedProductCount,
                        ReturnedProductCount = response.ProductCounts.ReturnedProductCount,
                        RequestedProductsAlreadyUpToDateCount = response.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                        RequestedProductsNotReturned = response.ProductCounts.RequestedProductsNotReturned?.Select(r => new RequestedProductsNotReturned
                        {
                            // Map properties as needed

                        }).ToList() ?? new List<RequestedProductsNotReturned>()
                    },
                    ResponseCode = HttpStatusCode.OK
                };
            }

            catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotModified)
            {
                return new S100ProductNamesResponse
                {
                    Products = new List<S100ProductNames>(),
                    ProductCounts = new UKHO.ADDS.Clients.SalesCatalogueService.Models.ProductCounts(),
                    ResponseCode = HttpStatusCode.NotModified
                };
            }

            catch (Exception ex)
            {
                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return new S100ProductNamesResponse();
            }
        }

    }
}

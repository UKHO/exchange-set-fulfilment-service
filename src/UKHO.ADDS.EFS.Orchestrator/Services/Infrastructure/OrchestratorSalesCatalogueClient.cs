using System.Net;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Adapters.Products;
using UKHO.ADDS.EFS.Domain.Services.Implementation.Retries;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Products;
using UKHO.ADDS.Infrastructure.Results;

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
        public OrchestratorSalesCatalogueClient(
            KiotaSalesCatalogueService salesCatalogueClient,
            ILogger<OrchestratorSalesCatalogueClient> logger)
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
        public async Task<(ProductList s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductVersionListAsync(
            DateTime? sinceDateTime,
            Job job)
        {
            var headersOption = new HeadersInspectionHandlerOption
            {
                InspectResponseHeaders = true
            };

            try
            {
                var headerDateString = sinceDateTime?.ToString("R");
                var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductVersionListAsync));

                var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _salesCatalogueClient.V2.Catalogues.S100.Basic.GetAsync(config =>
                    {
                        if (!string.IsNullOrEmpty(headerDateString))
                        {
                            config.Headers.Add("If-Modified-Since", headerDateString);
                        }

                        config.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                        config.Options.Add(headersOption);
                    });

                    return Result.Success(result);
                });

                var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                    ? values.FirstOrDefault()
                    : null;

                DateTime.TryParse(lastModifiedHeader, out var lastModifiedActual);

                if (s100BasicCatalogueResult.IsSuccess(out var catalogueList) && catalogueList is not null)
                {
                    var response = catalogueList.ToDomain(lastModifiedActual);
                    return (response, response.LastModified);
                }

                _logger.LogSalesCatalogueApiError(SalesCatalogApiErrorLogView.Create(job));
                return (new ProductList(), sinceDateTime);
            }
            catch (ApiException apiException)
            {
                switch (apiException.ResponseStatusCode)
                {
                    case (int)HttpStatusCode.NotModified:
                        {
                            var lastModifiedHeader = headersOption.ResponseHeaders.TryGetValue("Last-Modified", out var values)
                                ? values.FirstOrDefault()
                                : null;

                            if (!DateTime.TryParse(lastModifiedHeader, out var parsed))
                            {
                                // Fall back if header missing or unparsable
                                parsed = sinceDateTime ?? default;
                            }

                            return (new ProductList
                            {
                                ResponseCode = HttpStatusCode.NotModified
                            }, parsed);
                        }

                    default:
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, (HttpStatusCode)apiException.ResponseStatusCode));
                        return (new ProductList(), sinceDateTime);
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
        public async Task<ProductEditionList> GetS100ProductEditionListAsync(
            IEnumerable<ProductName> productNames,
            Job job,
            CancellationToken cancellationToken)
        {
            try
            {
                var retryPolicy =
                    HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100ProductResponse?>(_logger, nameof(GetS100ProductEditionListAsync));

                var s100ProductNamesResult = await retryPolicy.ExecuteAsync(async () =>
                {
                    var payload = productNames.Select(x => (string)x).ToList();

                    var result = await _salesCatalogueClient.V2.Products.S100.ProductNames.PostAsync(payload,
                        requestConfiguration =>
                        {
                            requestConfiguration.Headers.Add("X-Correlation-Id", (string)job.GetCorrelationId());
                        },
                        cancellationToken);

                    return Result.Success(result);
                });

                if (s100ProductNamesResult.IsSuccess(out var response) && response is not null)
                {
                    return response.ToDomain();
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

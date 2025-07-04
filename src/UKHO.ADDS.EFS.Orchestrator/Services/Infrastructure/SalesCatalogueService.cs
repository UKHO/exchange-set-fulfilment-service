using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.RetryPolicy;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    /// <summary>
    ///     Service responsible for retrieving product information from the Sales Catalogue.
    /// </summary>
    internal class SalesCatalogueService
    {
        private const string ScsApiVersion = "v2";
        private const string ProductType = "s100";
        private readonly ILogger<SalesCatalogueService> _logger;
        private readonly ISalesCatalogueClient _salesCatalogueClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SalesCatalogueService" /> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public SalesCatalogueService(ISalesCatalogueClient salesCatalogueClient, ILogger<SalesCatalogueService> logger)
        {
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Retrieves S100 products that have been modified since a specific date.
        /// </summary>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="job">The exchange set request message containing correlation ID and other metadata.</param>
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
        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(DateTime? sinceDateTime, ExchangeSetJob job)
        {
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<S100SalesCatalogueResponse>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
            var s100SalesCatalogueResult = await retryPolicy.ExecuteAsync(() =>
                _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(ScsApiVersion, ProductType, sinceDateTime, job.GetCorrelationId()));

            // Check if the API call was successful
            if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            {
                // Process the response based on the HTTP status code
                switch (s100SalesCatalogueData.ResponseCode)
                {
                    case HttpStatusCode.OK:
                        // Return the response data with the last modified timestamp from the API
                        return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

                    case HttpStatusCode.NotModified:
                        // No changes since the provided timestamp, return the original response with the provided timestamp
                        return (s100SalesCatalogueData, sinceDateTime);

                    default:
                        // Unexpected status code, log a warning and return an empty response
                        _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, s100SalesCatalogueData.ResponseCode));
                        return (new S100SalesCatalogueResponse(), sinceDateTime);
                }
            }

            // API call failed, log the error 
            _logger.LogSalesCatalogueApiError(error, SalesCatalogApiErrorLogView.Create(job));

            // Return an empty response with the original timestamp in case of failure
            return (new S100SalesCatalogueResponse(), sinceDateTime);
        }
    }
}

using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    /// <summary>
    /// Service responsible for retrieving product information from the Sales Catalogue.
    /// </summary>
    public class SalesCatalogueService : ISalesCatalogueService
    {
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<SalesCatalogueService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SalesCatalogueService"/> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public SalesCatalogueService(ISalesCatalogueClient salesCatalogueClient, ILogger<SalesCatalogueService> logger)
        {
            _salesCatalogueClient = salesCatalogueClient ?? throw new ArgumentNullException(nameof(salesCatalogueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves S100 products that have been modified since a specific date.
        /// </summary>
        /// <param name="apiVersion">The version of the Sales Catalogue API to use.</param>
        /// <param name="productType">The type of products to retrieve.</param>
        /// <param name="sinceDateTime">Optional date and time to filter products that have changed since this time.</param>
        /// <param name="message">The exchange set request message containing correlation ID and other metadata.</param>
        /// <returns>
        /// A tuple containing:
        /// - s100SalesCatalogueData: The response from the Sales Catalogue API.
        /// - LastModified: The timestamp when the data was last modified. Will be the original sinceDateTime if response is NotModified.
        /// </returns>
        /// <remarks>
        /// The method returns an empty response with the original sinceDateTime when an error occurs or when
        /// an unexpected HTTP status code is returned from the API.
        /// </remarks>
        public async Task<(S100SalesCatalogueResponse s100SalesCatalogueData, DateTime? LastModified)> GetS100ProductsFromSpecificDateAsync(string apiVersion,
                string productType,
                DateTime? sinceDateTime,
                ExchangeSetRequestQueueMessage message)
        {
            // Call the Sales Catalogue API to retrieve product information
            var s100SalesCatalogueResult = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, message.CorrelationId);

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
                        LogSalesCatalogueUnexpectedStatusCode(s100SalesCatalogueData.ResponseCode, message.CorrelationId, message);
                        return (new S100SalesCatalogueResponse(), sinceDateTime);
                }
            }
            else
            {
                // API call failed, log the error using the extension method from OrchestratorLogs
                _logger.LogSalesCatalogueError(error, message);
            }

            // Return an empty response with the original timestamp in case of failure
            return (new S100SalesCatalogueResponse(), sinceDateTime);
        }
        
        private void LogSalesCatalogueUnexpectedStatusCode(HttpStatusCode responseCode, string correlationId, ExchangeSetRequestQueueMessage message)
        {
            var salesCatalogueLogView = new SalesCatalogueServiceLog
            {
                ResponseCode = responseCode,
                CorrelationId = correlationId,
                Message = message,
            };

            _logger.LogUnexpectedSalesCatalogueStatusCode(salesCatalogueLogView);
        }
    }
}

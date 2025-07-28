using System.Net;
using System.Net.Http;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builds.S100;
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
        private readonly KiotaSalesCatalogueService _kiotaSalesCatalogueService;
        private readonly HeadersInspectionHandlerOption _headersInspectionHandlerOption;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrchestratorSalesCatalogueClient" /> class.
        /// </summary>
        /// <param name="salesCatalogueClient">Client for interacting with the Sales Catalogue API.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public OrchestratorSalesCatalogueClient(KiotaSalesCatalogueService kiotaSalesCatalogueService, HeadersInspectionHandlerOption headersInspectionHandlerOption, ILogger<OrchestratorSalesCatalogueClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _kiotaSalesCatalogueService = kiotaSalesCatalogueService ?? throw new ArgumentNullException(nameof(kiotaSalesCatalogueService));
            _headersInspectionHandlerOption = headersInspectionHandlerOption;
        }

        public static async Task<IResult<T>> KiotaRunnerAsync<T>(Func<Task<T>> kiotaTask, string correlationId)
        {
            try
            {
                var result = await kiotaTask();

                return Result.Success(result);
            }
            catch (ApiException ex)
            {
                var correlationIdProperty = ex.GetType().GetProperty("correlationId");
                if (correlationIdProperty != null)
                {
                    correlationId = correlationIdProperty.GetValue(ex)?.ToString();
                }

                if((HttpStatusCode)ex.ResponseStatusCode == HttpStatusCode.NotModified)
                {
                    // If the response is NotModified, we can return a success result with null data
                    return Result.Success<T>(default);
                }

                return Result.Failure<T>(ErrorFactory.CreateError(
                    (HttpStatusCode)ex.ResponseStatusCode,
                    ex.Message,
                    ErrorFactory.CreateProperties(correlationId)));
            }
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
            var headerDateString = sinceDateTime!.Value.ToString("R");

            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
            var s100SalesCatalogueResult = await retryPolicy.ExecuteAsync(() =>
                 KiotaRunnerAsync(() => _kiotaSalesCatalogueService.V2.Catalogues.S100.Basic.GetAsync(configuration =>
                 {
                     configuration.Headers.Add("If-Modified-Since", headerDateString);
                     configuration.Options.Add(_headersInspectionHandlerOption);

                 }), job.GetCorrelationId()));

            var ReponseHeaders = _headersInspectionHandlerOption.ResponseHeaders;

            // Check if the API call was successful
            //if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            //{
            //    // Process the response based on the HTTP status code
            //    switch (s100SalesCatalogueData.ResponseCode)
            //    {
            //        case HttpStatusCode.OK:
            //            // Return the response data with the last modified timestamp from the API
            //            return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

            //        case HttpStatusCode.NotModified:
            //            // No changes since the provided timestamp, return the response data with the last modified timestamp from the API
            //            return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

            //        default:
            //            // Unexpected status code, log a warning and return an empty response
            //            _logger.LogUnexpectedSalesCatalogueStatusCode(SalesCatalogUnexpectedStatusLogView.Create(job, s100SalesCatalogueData.ResponseCode));
            //            return (new S100SalesCatalogueResponse(), sinceDateTime);
            //    }
            //}

            // API call failed, log the error 
            //_logger.LogSalesCatalogueApiError(error, SalesCatalogApiErrorLogView.Create(job));

            // Return an empty response with the original timestamp in case of failure
            return (new S100SalesCatalogueResponse(), sinceDateTime);
        }
    }
}

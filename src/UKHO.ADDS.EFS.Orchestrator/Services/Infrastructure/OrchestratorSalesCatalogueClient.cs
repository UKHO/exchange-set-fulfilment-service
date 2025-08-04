using System.Net;
using Microsoft.Kiota.Abstractions;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

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
        private readonly IKiotaSalesCatalogueService _kiotaSalesCatalogueService;
        private readonly IHeadersInspectionHandlerOption _headersInspectionHandlerOption;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrchestratorSalesCatalogueClient" /> class.
        /// </summary>
        /// <param name="kiotaSalesCatalogueService">The Kiota sales catalogue service.</param>
        /// <param name="headersInspectionHandlerOption">The headers inspection handler options.</param>
        /// <param name="logger">Logger for recording diagnostic information.</param>
        public OrchestratorSalesCatalogueClient(IKiotaSalesCatalogueService kiotaSalesCatalogueService, IHeadersInspectionHandlerOption headersInspectionHandlerOption, ILogger<OrchestratorSalesCatalogueClient> logger)
        {
            _kiotaSalesCatalogueService = kiotaSalesCatalogueService ?? throw new ArgumentNullException(nameof(kiotaSalesCatalogueService));
            _headersInspectionHandlerOption = headersInspectionHandlerOption ?? throw new ArgumentNullException(nameof(headersInspectionHandlerOption));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                if ((HttpStatusCode)ex.ResponseStatusCode == HttpStatusCode.NotModified)
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

            // Enable response headers inspection to capture Last-Modified header
            _headersInspectionHandlerOption.InspectResponseHeaders = true;

            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<List<S100BasicCatalogue>?>(_logger, nameof(GetS100ProductsFromSpecificDateAsync));
            var s100BasicCatalogueResult = await retryPolicy.ExecuteAsync(() =>
                 KiotaRunnerAsync(() => _kiotaSalesCatalogueService.V2.Catalogues.S100.Basic.GetAsync(configuration =>
                 {
                     configuration.Headers.Add("If-Modified-Since", headerDateString);
                     configuration.Options.Add(_headersInspectionHandlerOption);
                 }), job.GetCorrelationId()));

            // The response headers are available through the headers inspection handler option
            var responseHeaders = _headersInspectionHandlerOption.ResponseHeaders;


            if (s100BasicCatalogueResult.IsSuccess(out var s100BasicCatalogues, out var error))
            {
                var a = s100BasicCatalogues;
            }
            
            //// Check if the API call was successful
            //if (s100SalesCatalogueResult.IsSuccess(out var s100CatalogueList, out var error))
            //{
            //    // Extract Last-Modified header from response
            //    DateTime? lastModified = null;
            //    if (responseHeaders?.TryGetValue("Last-Modified", out var lastModifiedValues) == true)
            //    {
            //        var lastModifiedValue = lastModifiedValues.FirstOrDefault();
            //        if (!string.IsNullOrEmpty(lastModifiedValue) && DateTime.TryParse(lastModifiedValue, out var parsedDate))
            //        {
            ////            lastModified = parsedDate;
            ////        }
            ////    }

            ////    // For Kiota responses, we need to determine the status code from the result
            ////    // If we got data successfully, assume it's OK (200)
            ////    var responseWrapper = new S100SalesCatalogueResponse
            ////    {
            ////        ResponseCode = HttpStatusCode.OK,
            ////        LastModified = lastModified ?? sinceDateTime,
            ////        Products = s100CatalogueList ?? new List<S100BasicCatalogue>()
            ////    };

            //    return (responseWrapper, lastModified ?? sinceDateTime);
            //}

            //// API call failed, log the error 
            //_logger.LogSalesCatalogueApiError(error, SalesCatalogApiErrorLogView.Create(job));

            // Return an empty response with the original timestamp in case of failure
            return (new S100SalesCatalogueResponse(), sinceDateTime);
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
        ///     This is a placeholder implementation - the actual endpoint needs to be determined based on the Kiota-generated client.
        /// </remarks>
        public async Task<S100ProductNamesResponse> GetS100ProductNamesAsync(IEnumerable<string> productNames, Job job, CancellationToken cancellationToken)
        {
            // TODO: This method needs to be implemented with the correct Kiota endpoint
            // The exact endpoint structure depends on the generated Kiota client for the Sales Catalogue API
            // For now, returning an empty response to avoid compilation errors
            
           // _logger.LogWarning("GetS100ProductNamesAsync method not yet implemented with correct Kiota endpoint");
            
            return new S100ProductNamesResponse();
        }

        private async Task<IResult<S100SalesCatalogueResponse>> CreateS100ProductsFromSpecificDateResponse(List<S100BasicCatalogue> s100BasicCatalogues, IDictionary<string, IEnumerable<string>> responseHeaders, string correlationId)
        {
            var response = new S100SalesCatalogueResponse();

            //if (httpResponseMessage.StatusCode != HttpStatusCode.OK && httpResponseMessage.StatusCode != HttpStatusCode.NotModified)
            //{
            //    var errorMetadata = await httpResponseMessage.CreateErrorMetadata(ApiNames.SaleCatalogueService, correlationId);
            //    return Result.Failure<S100SalesCatalogueResponse>(ErrorFactory.CreateError(httpResponseMessage.StatusCode, errorMetadata));
            //}
            //else
            //{
            //    response.ResponseCode = httpResponseMessage.StatusCode;

            //    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            //    {
            //        var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            //        var products = JsonCodec.Decode<List<S100Products>>(bodyJson);

            //        response.ResponseBody = products ?? new List<S100Products>();
            //    }

            //    // Get LastModified header value from content headers if available
            //    if (httpResponseMessage.Content.Headers.LastModified is DateTimeOffset lastModified)
            //    {
            //        response.LastModified = lastModified.UtcDateTime;
            //    }
            //}
            return Result.Success(response);
        }
    }
}

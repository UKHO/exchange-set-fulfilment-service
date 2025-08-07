using System.Net;
using System.Text;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider;
        }

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }

        /// <summary>
        /// Retrieves S100 products from a specific date asynchronously.
        /// </summary>
        /// <param name="apiVersion">The API version to use.</param>
        /// <param name="productType">The type of product to retrieve.</param>
        /// <param name="sinceDateTime">The date and time since when the products are to be retrieved.</param>
        /// <param name="correlationId">The correlation ID for the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the S100 sales catalogue response.</returns>
        public async Task<IResult<S100SalesCatalogueResponse>> GetS100ProductsFromSpecificDateAsync(string apiVersion, string productType, DateTime? sinceDateTime, string correlationId)
        {
            var uri = new Uri($"{apiVersion}/catalogues/{productType}/basic", UriKind.Relative);

            try
            {
                using var httpClient = await GetAuthenticatedClientAsync(correlationId);

                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

                //set "If-Modified-Since" header value in request header if value is passed
                if (sinceDateTime.HasValue)
                {
                    var headerDateString = sinceDateTime.Value.ToString("R");
                    httpRequestMessage.Headers.Add("If-Modified-Since", headerDateString);
                }

                var response = await httpClient.SendAsync(httpRequestMessage);

                //create response result as per expected output by reading additional values from httpresponse object 
                return await CreateS100ProductsFromSpecificDateResponse(response, correlationId);

            }
            catch (Exception ex)
            {
                return Result.Failure<S100SalesCatalogueResponse>(ex);
            }
        }

        public async Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId)
        {
            // Implementation for GetProductsFromSpecificDateAsync
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId)
        {
            var code = HttpStatusCode.BadRequest;
            return await Task.FromResult(Result.Failure<SalesCatalogueResponse>(ErrorFactory.CreateError(code)));
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId)
        {
            try
            {
                return await Task.FromResult(Result.Success(new SalesCatalogueResponse()));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(Result.Failure<SalesCatalogueResponse>(ex));
            }
        }

        public async Task<IResult<SalesCatalogueDataResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            return await Task.FromResult(Result.Success(new SalesCatalogueDataResponse()));
        }

        /// <summary>
        /// Retrieves S100 product names asynchronously.
        /// </summary>
        /// <param name="apiVersion">The API version to use.</param>
        /// <param name="productType">The type of product to retrieve.</param>
        /// <param name="productNames">Enumerable of product names to retrieve information for.</param>
        /// <param name="correlationId">The correlation ID for the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the S100 product names response.</returns>
        public async Task<IResult<S100ProductNamesResponse>> GetS100ProductNamesAsync(string apiVersion, string productType, IEnumerable<string> productNames, string correlationId, CancellationToken cancellationToken = default)
        {
            var uri = new Uri($"{apiVersion}/products/{productType}/productNames", UriKind.Relative);

            try
            {
                using var httpClient = await GetAuthenticatedClientAsync(correlationId);
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

                var json = JsonCodec.Encode(productNames.ToList());
                httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var errorMetadata = await response.CreateErrorMetadata(ApiNames.SaleCatalogueService, correlationId);
                    return Result.Failure<S100ProductNamesResponse>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }

                var bodyJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var productNamesResponse = JsonCodec.Decode<S100ProductNamesResponse>(bodyJson);

                if (productNamesResponse != null)
                {
                    productNamesResponse.ResponseCode = response.StatusCode;
                    return Result.Success(productNamesResponse);
                }

                return Result.Failure<S100ProductNamesResponse>(ErrorFactory.CreateError(HttpStatusCode.BadRequest,
                    new Dictionary<string, object> { { "reason", "Failed to deserialize response" } }));

            }
            catch (Exception ex)
            {
                return Result.Failure<S100ProductNamesResponse>(ex);
            }
        }

        /// <summary>
        /// Creates a response for S100 products retrieved from a specific date.
        /// </summary>
        /// <param name="httpResponseMessage">The HTTP response message.</param>
        /// <returns>The result contains the S100 sales catalogue response.</returns>
        private async Task<IResult<S100SalesCatalogueResponse>> CreateS100ProductsFromSpecificDateResponse(HttpResponseMessage httpResponseMessage, string correlationId)
        {
            var response = new S100SalesCatalogueResponse();

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK && httpResponseMessage.StatusCode != HttpStatusCode.NotModified)
            {
                var errorMetadata = await httpResponseMessage.CreateErrorMetadata(ApiNames.SaleCatalogueService, correlationId);
                return Result.Failure<S100SalesCatalogueResponse>(ErrorFactory.CreateError(httpResponseMessage.StatusCode, errorMetadata));
            }
            else
            {
                response.ResponseCode = httpResponseMessage.StatusCode;

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
                    var products = JsonCodec.Decode<List<S100Products>>(bodyJson);

                    response.ResponseBody = products ?? new List<S100Products>();
                }

                // Get LastModified header value from content headers if available
                if (httpResponseMessage.Content.Headers.LastModified is DateTimeOffset lastModified)
                {
                    response.LastModified = lastModified.UtcDateTime;
                }
            }
            return Result.Success(response);
        }

        protected async Task<HttpClient> GetAuthenticatedClientAsync(string correlationId)
        {
            var httpClient = _httpClientFactory.CreateClient();

            await httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            httpClient.SetCorrelationIdHeader(correlationId);

            return httpClient;
        }
    }
}

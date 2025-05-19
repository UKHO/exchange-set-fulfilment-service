using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    /// <summary>
    /// Client for interacting with the IIC Tool API for exchange set operations.
    /// </summary>
    public class ToolClient : IToolClient
    {
        private readonly HttpClient _httpClient;
        private const string WorkSpaceId = "working9";
        private const string WorkSpaceRootPath = @"/usr/local/tomcat/ROOT/spool";
        private const string ApiVersion = "2.7";

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used for API requests.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="httpClient"/> is null.</exception>
        public ToolClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Pings the IIC Tool API to verify connectivity.
        /// </summary>
        /// <returns>
        /// A result indicating whether the ping was successful.
        /// Returns <c>true</c> if the API is reachable; otherwise, returns a failure result.
        /// </returns>
        public async Task<IResult<bool>> PingAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync($"/xchg-{ApiVersion}/v{ApiVersion}/dev?arg=test&authkey=noauth");
                response.EnsureSuccessStatusCode();
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                return Result.Failure<bool>(ex);
            }
        }

        /// <summary>
        /// Adds a new exchange set to the workspace.
        /// </summary>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="correlationId">The correlation ID for tracking.</param>
        /// <returns>A result containing the operation response.</returns>
        public Task<IResult<OperationResponse>> AddExchangeSetAsync(string exchangeSetId, string authKey, string correlationId) =>
            SendApiRequestAsync<OperationResponse>("addExchangeSet", exchangeSetId, authKey, correlationId);

        /// <summary>
        /// Adds content to an existing exchange set.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource to add.</param>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="correlationId">The correlation ID for tracking.</param>
        /// <returns>A result containing the operation response.</returns>
        public async Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, string exchangeSetId, string authKey, string correlationId)
        {
            var directoryName = Path.Combine("fss-data", Path.GetFileName(resourceLocation));
            return await SendApiRequestAsync<OperationResponse>("addContent", exchangeSetId, authKey, correlationId, directoryName);
        }

        /// <summary>
        /// Signs an exchange set.
        /// </summary>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="correlationId">The correlation ID for tracking.</param>
        /// <returns>A result containing the signing response.</returns>
        public Task<IResult<SigningResponse>> SignExchangeSetAsync(string exchangeSetId, string authKey, string correlationId) =>
            SendApiRequestAsync<SigningResponse>("signExchangeSet", exchangeSetId, authKey, correlationId);

        /// <summary>
        /// Extracts an exchange set as a stream.
        /// </summary>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="correlationId">The correlation ID for tracking.</param>
        /// <returns>A result containing the extracted stream.</returns>
        public async Task<IResult<Stream>> ExtractExchangeSetAsync(string exchangeSetId, string authKey, string correlationId)
        {
            try
            {
                var path = BuildApiPath("extractExchangeSet", exchangeSetId, authKey);
                var response = await _httpClient.GetAsync(path);
                return await response.CreateResultAsync<Stream>("IICToolAPI", correlationId);
            }
            catch (Exception ex)
            {
                return Result.Failure<Stream>(ex);
            }
        }

        /// <summary>
        /// Lists the contents of the workspace.
        /// </summary>
        /// <param name="authKey">The authentication key.</param>
        /// <returns>A result containing the workspace listing as a string.</returns>
        public async Task<IResult<string>> ListWorkspaceAsync(string authKey)
        {
            try
            {
                var path = $"/xchg-{ApiVersion}/v{ApiVersion}/listWorkspace?authkey={authKey}";
                using var response = await _httpClient.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Result.Success(content);
                }
                else
                {
                    var errorMetadata = await response.CreateErrorMetadata("IICToolAPI", "correlationId");
                    return Result.Failure<string>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<string>(ex);
            }
        }

        /// <summary>
        /// Sends an API request to the IIC Tool API and parses the response.
        /// </summary>
        /// <typeparam name="T">The type of the expected response object.</typeparam>
        /// <param name="action">The API action to perform.</param>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="correlationId">The correlation ID for tracking.</param>
        /// <param name="resourceLocation">Optional resource location parameter.</param>
        /// <returns>A result containing the deserialized response object.</returns>
        private async Task<IResult<T>> SendApiRequestAsync<T>(string action, string exchangeSetId, string authKey, string correlationId, string? resourceLocation = null)
        {
            try
            {
                var path = BuildApiPath(action, exchangeSetId, authKey, resourceLocation);
                using var response = await _httpClient.GetAsync(path);
                var content = await response.Content.ReadAsStringAsync();
                var resultObj = JsonCodec.Decode<T>(content);

                if (response.IsSuccessStatusCode && resultObj != null)
                {
                    return Result.Success(resultObj);
                }
                else
                {
                    var errorMetadata = await response.CreateErrorMetadata("IICToolAPI", correlationId);
                    return Result.Failure<T>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<T>(ex);
            }
        }

        /// <summary>
        /// Builds the API path for a given action and parameters.
        /// </summary>
        /// <param name="action">The API action.</param>
        /// <param name="exchangeSetId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="resourceLocation">Optional resource location parameter.</param>
        /// <returns>The constructed API path.</returns>
        private string BuildApiPath(string action, string exchangeSetId, string authKey, string? resourceLocation = null)
        {
            var basePath = $"/xchg-{ApiVersion}/v{ApiVersion}/{action}/{WorkSpaceId}/{exchangeSetId}";
            var query = $"?authkey={authKey}";
            if (!string.IsNullOrEmpty(resourceLocation))
            {
                query += $"&resourceLocation={resourceLocation}";
            }
            return basePath + query;
        }
    }
}

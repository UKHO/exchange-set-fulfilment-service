using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Domain.Jobs;
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
        private const string ApiVersion = "7.4";
        private const string ApplicationName = "IICToolAPI";
        private const string AddExchangeSet = "addExchangeSet";
        private const string AddContent = "addContent";        

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
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <returns>A result containing the operation response.</returns>
        public Task<IResult<OperationResponse>> AddExchangeSetAsync(JobId jobId, string authKey) =>
            SendApiRequestAsync<OperationResponse>("addExchangeSet", jobId, authKey);

        /// <summary>
        /// Adds content to an existing exchange set.
        /// </summary>
        /// <param name="resourceLocation">The location of the resource to add.</param>
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <returns>A result containing the operation response.</returns>
        public async Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, JobId jobId, string authKey)
        {
            return await SendApiRequestAsync<OperationResponse>("addContent", jobId, authKey, resourceLocation);
        }

        /// <summary>
        /// Signs an exchange set.
        /// </summary>
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <returns>A result containing the signing response.</returns>
        public Task<IResult<SigningResponse>> SignExchangeSetAsync(JobId jobId, string authKey) =>
            SendApiRequestAsync<SigningResponse>("signExchangeSet", jobId, authKey);

        /// <summary>
        /// Extracts an exchange set as a stream.
        /// </summary>
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="destination">The destination location.</param>
        /// <returns>A result containing the extracted stream.</returns>
        public async Task<IResult<Stream>> ExtractExchangeSetAsync(JobId jobId, string authKey, string destination)
        {
            try
            {
                var path = BuildApiPath("extractExchangeSet", jobId, authKey, null, destination);
                var response = await _httpClient.GetAsync(path);
                return await response.CreateResultAsync<Stream>(ApplicationName, (string)jobId);
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
                    return Result.Failure<string>(ErrorFactory.CreateError(response.StatusCode));
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
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="resourceLocation">Optional resource location parameter.</param>
        /// <returns>A result containing the deserialized response object.</returns>
        private async Task<IResult<T>> SendApiRequestAsync<T>(string action, JobId jobId, string authKey, string? resourceLocation = null)
        {
            try
            {
                var path = BuildApiPath(action, jobId, authKey, resourceLocation);
                using var response = await SendHttpRequestAsync(action, path);
                var content = await response.Content.ReadAsStringAsync();
                var resultObj = JsonCodec.Decode<T>(content);

                if (response.IsSuccessStatusCode && resultObj != null)
                {
                    return Result.Success(resultObj);
                }

                var errorMetadata = await response.CreateErrorMetadata(ApplicationName, (string)jobId);
                return Result.Failure<T>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
            }
            catch (Exception ex)
            {
                return Result.Failure<T>(ex);
            }
        }

        /// <summary>
        /// Sends an HTTP request to the IIC Tool API for the specified action and path.
        /// </summary>
        /// <param name="action">The API action to perform (e.g., "addExchangeSet", "addContent", etc.).</param>
        /// <param name="path">The fully constructed API endpoint path.</param>
        /// <returns>returns the response from the API.</returns>
        private async Task<HttpResponseMessage> SendHttpRequestAsync(string action, string path)
        {
            if (action == AddExchangeSet || action == AddContent)
            {
                var emptyContent = new StringContent(string.Empty, System.Text.Encoding.UTF8,ApiHeaderKeys.ContentTypeJson );
                return await _httpClient.PutAsync(path, emptyContent);
            }
            return await _httpClient.GetAsync(path);
        }

        /// <summary>
        /// Builds the API path for a given action and parameters.
        /// </summary>
        /// <param name="action">The API action.</param>
        /// <param name="jobId">The ID of the exchange set.</param>
        /// <param name="authKey">The authentication key.</param>
        /// <param name="resourceLocation">Optional resource location parameter.</param>
        /// <returns>The constructed API path.</returns>
        private string BuildApiPath(string action, JobId jobId, string authKey, string? resourceLocation = null, string? destination = null)
        {
            var basePath = $"/xchg-{ApiVersion}/v{ApiVersion}/{action}/{WorkSpaceId}/{jobId}";
            var query = $"?authkey={authKey}";
            if (!string.IsNullOrEmpty(resourceLocation))
            {
                query += $"&resourceLocation={resourceLocation}";
            }
            if (!string.IsNullOrEmpty(destination))
            {
                query += $"&destination={destination}";
            }
            return basePath + query;
        }
    }
}

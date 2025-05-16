using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    public class ToolClient : IToolClient
    {
        private readonly HttpClient _httpClient;
        private const string WorkSpaceId = "working9";
        private const string WorkSpaceRootPath = @"/usr/local/tomcat/ROOT/spool";
        private const string ApiVersion = "2.7";

        public ToolClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task PingAsync()
        {
            using var response = await _httpClient.GetAsync($"/xchg-{ApiVersion}/v{ApiVersion}/dev?arg=test&authkey=noauth");
            response.EnsureSuccessStatusCode();
        }

        public Task<IResult<OperationResponse>> AddExchangeSetAsync(string exchangeSetId, string authKey, string correlationId) =>
            SendApiRequestAsync<OperationResponse>("addExchangeSet", exchangeSetId, authKey, correlationId);

        public async Task<IResult<OperationResponse>> AddContentAsync(string resourceLocation, string exchangeSetId, string authKey, string correlationId)
        {
            var directoryName = Path.Combine("fss-data", Path.GetFileName(resourceLocation));
            return await SendApiRequestAsync<OperationResponse>("addContent", exchangeSetId, authKey, correlationId, directoryName);
        }

        public Task<IResult<SigningResponse>> SignExchangeSetAsync(string exchangeSetId, string authKey, string correlationId) =>
            SendApiRequestAsync<SigningResponse>("signExchangeSet", exchangeSetId, authKey, correlationId);

        public async Task<IResult<Stream>> ExtractExchangeSetAsync(string exchangeSetId, string authKey, string correlationId)
        {
            try
            {
                var path = BuildApiPath("extractExchangeSet", exchangeSetId, authKey);
                using var response = await _httpClient.GetAsync(path);
                return await response.CreateResultAsync<Stream>("IICToolAPI", correlationId);
            }
            catch (Exception ex)
            {
                return Result.Failure<Stream>(ex);
            }
        }

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

        private string BuildApiPath(string action, string exchangeSetId, string authKey, string? resourceLocation = null)
        {
            var basePath = $"/xchg-{ApiVersion}/v{ApiVersion}/{action}/{WorkSpaceId}/{exchangeSetId}";
            var query = $"?authkey={authKey}";
            if (!string.IsNullOrEmpty(resourceLocation))
                query += $"&resourceLocation={resourceLocation}";
            return basePath + query;
        }

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

    }
}

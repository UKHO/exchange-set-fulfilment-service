using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly
{
    public class FileShareReadOnlyClient : IFileShareReadOnlyClient
    {
        private readonly int _maxDownloadBytes = 10485760;

        protected readonly IAuthenticationTokenProvider _authTokenProvider;
        protected readonly IHttpClientFactory _httpClientFactory;

        public FileShareReadOnlyClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            if (httpClientFactory == null)
                throw new ArgumentNullException(nameof(httpClientFactory));
            if (string.IsNullOrWhiteSpace(baseAddress))
                throw new UriFormatException("Invalid URI: The URI is empty.");
            if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
                throw new UriFormatException("Invalid URI: The format of the URI could not be determined.");

            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        }

        public FileShareReadOnlyClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }

        public async Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(string batchId)
        {
            var uri = $"batch/{batchId}/status";

            try
            {
                using var httpClient = await GetAuthenticationHeaderSetClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

                return await response.CreateResultAsync<BatchStatusResponse>();
            }
            catch (Exception ex)
            {
                return Result.Failure<BatchStatusResponse>(ex.Message);
            }
        }
        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery) => await SearchAsync(searchQuery, null, null, string.Empty);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize) => await SearchAsync(searchQuery, pageSize, null, string.Empty);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start) => await SearchAsync(searchQuery, pageSize, start, string.Empty, CancellationToken.None);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start,  CancellationToken cancellationToken)
        {
            var response = await SearchResponse(searchQuery, pageSize, start, string.Empty, cancellationToken);

            return await response.CreateResultAsync<BatchSearchResponse>(ApiNames.FileShareService);
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, string correlationId) => await SearchAsync(searchQuery, null, null, correlationId);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, string correlationId) => await SearchAsync(searchQuery, pageSize, null, correlationId);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, string correlationId) => await SearchAsync(searchQuery, pageSize, start, correlationId, CancellationToken.None);

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, string correlationId, CancellationToken cancellationToken)
        {
            var response = await SearchResponse(searchQuery, pageSize, start, correlationId, cancellationToken);

            return await response.CreateResultAsync<BatchSearchResponse>(ApiNames.FileShareService, correlationId);            
        }

        protected IError TranslateErrors(ErrorResponseModel errorResponseModel, HttpStatusCode status)
        {
            var errorProperties = ErrorFactory.CreateProperties(errorResponseModel.CorrelationId);

            if (errorResponseModel.Errors.Any())
            {
                errorProperties.Add("inner", errorProperties);
            }

            return ErrorFactory.CreateError(status, errorProperties);
        }

        public async Task<IResult<Stream>> DownloadFileAsync(string batchId, string filename)
        {
            var uri = $"batch/{batchId}/files/{filename}";

            try
            {
                using var httpClient = await GetAuthenticationHeaderSetClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
                return await response.CreateResultAsync<Stream>();
            }
            catch (Exception ex)
            {
                return Result.Failure<Stream>(ex.Message);
            }
        }

        public async Task<IResult<Stream>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, string correlationId, long fileSizeInBytes = 0, CancellationToken cancellationToken = default)
        {
            return await DownloadFileInternalAsync(batchId, fileName, destinationStream,
                cancellationToken, fileSizeInBytes, correlationId);
        }

        public async Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default)
        {
            long startByte = 0;
            var endByte = fileSizeInBytes < _maxDownloadBytes ? fileSizeInBytes - 1 : _maxDownloadBytes - 1;
            IResult<DownloadFileResponse> result = null;

            while (startByte <= endByte)
            {
                var rangeHeader = $"bytes={startByte}-{endByte}";

                var uri = $"batch/{batchId}/files/{fileName}";

                using (var httpClient = await GetAuthenticationHeaderSetClient())
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    if (fileSizeInBytes != 0 && rangeHeader != null)
                    {
                        httpRequestMessage.Headers.Add("Range", rangeHeader);
                    }

                    var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                    
                    result = await response.CreateResultAsync<DownloadFileResponse>();

                    if (!result.IsSuccess()) return result;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        contentStream.CopyTo(destinationStream);
                    }
                }

                startByte = endByte + 1;
                endByte += _maxDownloadBytes - 1;

                if (endByte > fileSizeInBytes - 1)
                {
                    endByte = fileSizeInBytes - 1;
                }
            }

            return result;
        }

        private async Task<IResult<Stream>> DownloadFileInternalAsync(string batchId, string fileName, Stream destinationStream, CancellationToken cancellationToken, long fileSizeInBytes, string? correlationId = null)
        {
            try
            {
                long startByte = 0;
                var endByte = fileSizeInBytes < _maxDownloadBytes ? fileSizeInBytes - 1 : _maxDownloadBytes - 1;

                while (startByte <= endByte)
                {
                    var rangeHeader = $"bytes={startByte}-{endByte}";

                    var uri = $"batch/{batchId}/files/{fileName}";

                    using (var httpClient = await CreateHttpClientWithHeadersAsync(correlationId))
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        if (fileSizeInBytes != 0 && rangeHeader != null)
                        {
                            httpRequestMessage.Headers.Add("Range", rangeHeader);
                        }

                        var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                        var result = await response.CreateResultAsync<Stream>(ApiNames.FileShareService, correlationId);

                        if (!result.IsSuccess())
                        {
                            return result;
                        }

                        await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                        {
                            await contentStream.CopyToAsync(destinationStream, cancellationToken);
                        }
                    }

                    startByte = endByte + 1;
                    endByte += _maxDownloadBytes - 1;

                    if (endByte > fileSizeInBytes - 1)
                    {
                        endByte = fileSizeInBytes - 1;
                    }
                }

                return Result.Success(destinationStream);
            }
            catch (Exception ex)
            {
                return Result.Failure<Stream>(ex);
            }
        }

        public async Task<IResult<IEnumerable<string>>> GetUserAttributesAsync()
        {
            var uri = "attributes";

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

                return await response.CreateResultAsync<IEnumerable<string>>();
            }
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken)
        {
            var uri = "attributes/search";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            uri = AddQueryString(uri, query);

            using var httpClient = await GetAuthenticationHeaderSetClient();
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            return await response.CreateResultAsync<BatchAttributesSearchResponse, ErrorResponseModel>(TranslateErrors);
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken)
        {
            var uri = "attributes/search";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            query["maxAttributeValueCount"] = maxAttributeValueCount.ToString();

            uri = AddQueryString(uri, query);

            using var httpClient = await GetAuthenticationHeaderSetClient();
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            return await response.CreateResultAsync<BatchAttributesSearchResponse, ErrorResponseModel>(TranslateErrors);
        }

        public async Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken)
        {
            var uri = $"batch/{batchId}/files";

            using var httpClient = await GetAuthenticationHeaderSetClient();
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            return await response.CreateResultAsync<Stream>();
        }

        protected async Task<HttpClient> GetAuthenticationHeaderSetClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            return httpClient;
        }

        protected async Task<HttpClient> CreateHttpClientWithHeadersAsync(string correlationId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpClient.SetCorrelationIdHeader(correlationId);
            }
            
            return httpClient;
        }

        private async Task<HttpResponseMessage> SearchResponse(string searchQuery, int? pageSize, int? start, string? correlationId, CancellationToken cancellationToken)
        {
            var uri = "batch";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            if (pageSize.HasValue)
            {
                if (pageSize <= 0)
                {
                    throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
                }

                query["limit"] = pageSize.Value + "";
            }

            if (start.HasValue)
            {
                if (start < 0)
                {
                    throw new ArgumentException("Start cannot be less than zero.", nameof(start));
                }

                query["start"] = start.Value + "";
            }

            uri = AddQueryString(uri, query);

            using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            return await httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }

        private static string AddQueryString(string uri, IEnumerable<KeyValuePair<string, string>> queryString)
        {
            var queryIndex = uri.IndexOf('?');
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(uri);

            foreach (var parameter in queryString)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(UrlEncoder.Default.Encode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncoder.Default.Encode(parameter.Value));

                hasQuery = true;
            }

            return sb.ToString();
        }
    }
}

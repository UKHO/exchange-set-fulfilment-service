using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public class FileShareReadWriteClient : FileShareReadOnlyClient, IFileShareReadWriteClient
    {
        private const int DefaultMaxFileBlockSize = 4194304;
        private readonly int _maxFileBlockSize;

        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
            : base(httpClientFactory, baseAddress, authTokenProvider)
        {
            if (httpClientFactory == null)
                throw new ArgumentNullException(nameof(httpClientFactory));
            if (string.IsNullOrWhiteSpace(baseAddress))
                throw new UriFormatException(nameof(baseAddress));
            if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
                throw new UriFormatException(nameof(baseAddress));

            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
            _maxFileBlockSize = DefaultMaxFileBlockSize;
        }

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken)) => _maxFileBlockSize = DefaultMaxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken, int maxFileBlockSize) : this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken)) => _maxFileBlockSize = maxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider, int maxFileBlockSize) : this(httpClientFactory, baseAddress, authTokenProvider) => _maxFileBlockSize = maxFileBlockSize;

        public Task<IResult<AppendAclResponse>> AppendAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<AppendAclResponse>>(Result.Success(new AppendAclResponse()));

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, CancellationToken cancellationToken = default)
        {
            return await CreateBatchInternalAsync(batchModel, cancellationToken);
        }

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, string correlationId, CancellationToken cancellationToken = default)
        {
            return await CreateBatchInternalAsync(batchModel, cancellationToken, correlationId);
        }

        public Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(IBatchHandle batchHandle) => Task.FromResult<IResult<BatchStatusResponse>>(Result.Success(new BatchStatusResponse()));

        public Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, string correlationId, params KeyValuePair<string, string>[] fileAttributes)
        {
            return AddFileToBatchAsync(batchHandle, stream, fileName, mimeType, _ => { }, correlationId, CancellationToken.None, fileAttributes);
        }

        public Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, string correlationId, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes)
        {
            return AddFileToBatchAsync(batchHandle, stream, fileName, mimeType, _ => { }, correlationId, cancellationToken, fileAttributes);
        }

        public async Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, params KeyValuePair<string, string>[] fileAttributes)
        {
            await AddFileAsync(batchHandle, stream, fileName, mimeType, progressUpdate, CancellationToken.None, fileAttributes);
        }

        public async Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, string correlationId, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes)
        {
            return await AddFiles(batchHandle, stream, fileName, mimeType, progressUpdate, cancellationToken, correlationId, fileAttributes);
        }

        public async Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken = default)
        {
            return await CommitBatchInternalAsync(batchHandle, cancellationToken);
        }

        public async Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, string correlationId, CancellationToken cancellationToken = default)
        {
            return await CommitBatchInternalAsync(batchHandle, cancellationToken, correlationId);
        }
        
        public Task<IResult<ReplaceAclResponse>> ReplaceAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<ReplaceAclResponse>>(Result.Success(new ReplaceAclResponse()));

        public Task<IResult> RollBackBatchAsync(IBatchHandle batchHandle) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<RollBackBatchResponse>> RollBackBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken) => Task.FromResult<IResult<RollBackBatchResponse>>(Result.Success(new RollBackBatchResponse()));

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default)
        {
            return await SetExpiryDateInternalAsync(batchId, batchExpiry, cancellationToken);
        }

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, string correlationId, CancellationToken cancellationToken = default)
        {
            return await SetExpiryDateInternalAsync(batchId, batchExpiry, cancellationToken ,correlationId);
        }

        private async Task<IResult<IBatchHandle>> CreateBatchInternalAsync(BatchModel batchModel, CancellationToken cancellationToken, string? correlationId = null)
        {
            var uri = new Uri("batch", UriKind.Relative);

            try
            {
                using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(JsonCodec.Encode(batchModel), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
                };

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId);
                    return Result.Failure<IBatchHandle>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }

                var batchHandle = await response.Content.ReadFromJsonAsync<BatchHandle>(cancellationToken: cancellationToken);
                return Result.Success<IBatchHandle>(batchHandle);
            }
            catch (Exception ex)
            {
                return Result.Failure<IBatchHandle>(ex.Message);
            }
        }

        private async Task<IResult<SetExpiryDateResponse>> SetExpiryDateInternalAsync(string batchId, BatchExpiryModel batchExpiryModel, CancellationToken cancellationToken, string? correlationId = null)
        {
            var uri = new Uri($"batch/{batchId}/expiry", UriKind.Relative);

            try
            {
                using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

                var formattedExpiryDate = batchExpiryModel.ExpiryDate?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
                {
                    Content = new StringContent(JsonCodec.Encode(new { ExpiryDate = formattedExpiryDate }), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
                };

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return Result.Success(new SetExpiryDateResponse() { IsExpiryDateSet = true });
                }

                var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId);
                return Result.Failure<SetExpiryDateResponse>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
            }
            catch (Exception ex)
            {
                return Result.Failure<SetExpiryDateResponse>(ex.Message);
            }
        }

        private async Task<IResult<CommitBatchResponse>> CommitBatchInternalAsync(IBatchHandle batchHandle, CancellationToken cancellationToken, string? correlationId = null)
        {
            var uri = new Uri($"batch/{batchHandle.BatchId}", UriKind.Relative);

            try
            {
                using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
                {
                    Content = new StringContent(JsonCodec.Encode(batchHandle), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson)
                };

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId);
                    return Result.Failure<CommitBatchResponse>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }

                var commitBatchResponse = await response.Content.ReadFromJsonAsync<CommitBatchResponse>(cancellationToken: cancellationToken);

                return Result.Success(commitBatchResponse);
            }
            catch (Exception ex)
            {
                return Result.Failure<CommitBatchResponse>(ex.Message);
            }
        }

        protected async Task<HttpClient> CreateHttpClientWithHeadersAsync(string? correlationId = null)
        {
            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpClient.SetCorrelationIdHeader(correlationId);
            }
            return httpClient;
        }

        private async Task AddFileAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] fileAttributes)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            stream.Seek(0, SeekOrigin.Begin);

            var fileUri = $"batch/{batchHandle.BatchId}/files/{fileName}";

            {
                var fileModel = new FileModel()
                { Attributes = fileAttributes ?? Enumerable.Empty<KeyValuePair<string, string>>() };

                var httpContent = new StringContent(JsonCodec.Encode(fileModel), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

                using (var httpClient = await GetAuthenticationHeaderSetClient())
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fileUri) { Content = httpContent })
                {
                    httpRequestMessage.Headers.Add(ApiHeaderKeys.ContentSizeHeaderKey, "" + stream.Length);

                    if (!string.IsNullOrEmpty(mimeType)) httpRequestMessage.Headers.Add(ApiHeaderKeys.MimeTypeHeaderKey, mimeType);

                    var createFileRecordResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                    createFileRecordResponse.EnsureSuccessStatusCode();
                }
            }

            var fileBlocks = new List<string>();
            var fileBlockId = 0;
            var expectedTotalBlockCount = (int)Math.Ceiling(stream.Length / (double)_maxFileBlockSize);
            progressUpdate((0, expectedTotalBlockCount));

            var buffer = new byte[_maxFileBlockSize];

            using (var md5 = MD5.Create())
            using (var cryptoStream = new CryptoStream(stream, md5, CryptoStreamMode.Read, true))
            {
                while (true)
                {
                    fileBlockId++;
                    var ms = new MemoryStream();

                    var read = cryptoStream.Read(buffer, 0, _maxFileBlockSize);
                    if (read <= 0) break;
                    ms.Write(buffer, 0, read);

                    var fileBlockIdAsString = fileBlockId.ToString("D5");
                    var putFileUri = $"batch/{batchHandle.BatchId}/files/{fileName}/{fileBlockIdAsString}";
                    fileBlocks.Add(fileBlockIdAsString);
                    ms.Seek(0, SeekOrigin.Begin);

                    var blockMD5 = ms.CalculateMd5();

                    using (var httpClient = await GetAuthenticationHeaderSetClient())
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, putFileUri) { Content = new StreamContent(ms) })
                    {
                        httpRequestMessage.Content.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(ApiHeaderKeys.ContentTypeOctetStream);

                        httpRequestMessage.Content.Headers.ContentMD5 = blockMD5;

                        var putFileResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                        putFileResponse.EnsureSuccessStatusCode();

                        progressUpdate((fileBlockId, expectedTotalBlockCount));
                    }
                }

                {
                    var writeBlockFileModel = new WriteBlockFileModel { BlockIds = fileBlocks };
                    var httpContent = new StringContent(JsonCodec.Encode(writeBlockFileModel), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

                    using (var httpClient = await GetAuthenticationHeaderSetClient())
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, fileUri) { Content = httpContent })
                    {
                        var writeFileResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                        writeFileResponse.EnsureSuccessStatusCode();
                    }
                }
                ((BatchHandle)batchHandle).AddFile(fileName, Convert.ToBase64String(md5.Hash));
            }
        }
        private async Task<IResult<AddFileToBatchResponse>> AddFiles(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
           Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken, string? correlationId = null,
           params KeyValuePair<string, string>[] fileAttributes)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            stream.Seek(0, SeekOrigin.Begin);

            var fileUri = $"batch/{batchHandle.BatchId}/files/{fileName}";
            var fileModel = new FileModel { Attributes = fileAttributes ?? Enumerable.Empty<KeyValuePair<string, string>>() };
            var requestHeaders = new Dictionary<string, string>
            {
                { ApiHeaderKeys.ContentSizeHeaderKey, stream.Length.ToString() }
            };
            if (!string.IsNullOrEmpty(mimeType))
                requestHeaders.Add(ApiHeaderKeys.MimeTypeHeaderKey, mimeType);

            var result = await SendResult<FileModel, AddFileToBatchResponse>(fileUri, HttpMethod.Post, fileModel, cancellationToken, correlationId, requestHeaders);
            if (result.Errors != null && result.Errors.Any())
                return (Result<AddFileToBatchResponse>)result;

            var fileBlocks = new List<string>();
            var fileBlockId = 0;
            var expectedTotalBlockCount = (int)Math.Ceiling(stream.Length / (double)_maxFileBlockSize);
            progressUpdate((0, expectedTotalBlockCount));
            var buffer = new byte[_maxFileBlockSize];

            using (var md5 = MD5.Create())
            using (var cryptoStream = new CryptoStream(stream, md5, CryptoStreamMode.Read, true))
            {
                while (true)
                {
                    fileBlockId++;
                    using (var ms = new MemoryStream())
                    {
                        var read = cryptoStream.Read(buffer, 0, _maxFileBlockSize);
                        if (read <= 0) break;
                        ms.Write(buffer, 0, read);

                        var fileBlockIdAsString = fileBlockId.ToString("D5");
                        var putFileUri = $"batch/{batchHandle.BatchId}/files/{fileName}/{fileBlockIdAsString}";
                        fileBlocks.Add(fileBlockIdAsString);
                        ms.Seek(0, SeekOrigin.Begin);

                        var blockMD5 = ms.CalculateMd5();
                        using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, putFileUri) { Content = new StreamContent(ms) })
                        {
                            httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ApiHeaderKeys.ContentTypeOctetStream);
                            httpRequestMessage.Content.Headers.ContentMD5 = blockMD5;
                            progressUpdate((fileBlockId, expectedTotalBlockCount));

                            var blockResult = await SendMessageResult<AddFileToBatchResponse>(httpRequestMessage, cancellationToken);
                            if (blockResult.Errors != null && blockResult.Errors.Any())
                                return (Result<AddFileToBatchResponse>)blockResult;
                        }
                    }
                }

                {
                    var writeBlockFileModel = new WriteBlockFileModel { BlockIds = fileBlocks };
                    var finalResult = await SendResult<WriteBlockFileModel, AddFileToBatchResponse>(fileUri, HttpMethod.Put, writeBlockFileModel, cancellationToken, correlationId);
                    if (finalResult.Errors != null && finalResult.Errors.Any())
                        return (Result<AddFileToBatchResponse>)finalResult;

                    ((BatchHandle)batchHandle).AddFile(fileName, Convert.ToBase64String(md5.Hash));
                    return finalResult;
                }
            }
        }

        private async Task<IResult<TResponse>> SendResult<TRequest, TResponse>(string uri, HttpMethod httpMethod, TRequest request, CancellationToken cancellationToken, string correlationId, Dictionary<string, string> requestHeaders = default)
            => await SendObjectResult<TResponse>(uri, httpMethod, request, cancellationToken, correlationId, requestHeaders);

        private async Task<IResult<TResponse>> SendObjectResult<TResponse>(string uri, HttpMethod httpMethod, object request, CancellationToken cancellationToken, string correlationId, Dictionary<string, string> requestHeaders = default)
        {
            var httpContent = new StringContent(JsonCodec.Encode(request), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

            using (var httpRequestMessage = new HttpRequestMessage(httpMethod, uri) { Content = httpContent })
            {
                foreach (var requestHeader in requestHeaders ?? new Dictionary<string, string>())
                {
                    httpRequestMessage.Headers.Add(requestHeader.Key, requestHeader.Value);
                }

                return await SendMessageResult<TResponse>(httpRequestMessage, cancellationToken, correlationId);
            }
        }

        private async Task<IResult<TResponse>> SendMessageResult<TResponse>(HttpRequestMessage messageToSend, CancellationToken cancellationToken, string? correlationId = null)
        {
            using (var httpClient = await CreateHttpClientWithHeadersAsync(correlationId))
            {
                var response = await httpClient.SendAsync(messageToSend, cancellationToken);
                var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId ?? string.Empty);
                    return Result.Failure<TResponse>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }

                if (string.IsNullOrWhiteSpace(contentString))
                {
                    return Result.Success<TResponse>(default);
                }

                var responseData = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
                return Result.Success(responseData);
            }
        }

    }
}

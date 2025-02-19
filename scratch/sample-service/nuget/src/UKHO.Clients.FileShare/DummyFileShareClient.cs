using UKHO.Clients.Common.Configuration;
using UKHO.Clients.FileShare.Models;
using UKHO.Infrastructure.Results;

namespace UKHO.Clients.FileShare
{
    internal class DummyFileShareClient : IFileShareClient
    {
        public DummyFileShareClient(ClientConfiguration configuration)
        {
        }

        public async Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(string batchId)
        {
            return Result.Success(new BatchStatusResponse());
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery)
        {
            return Result.Success(new BatchSearchResponse());
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize)
        {
            return Result.Success(new BatchSearchResponse());
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start)
        {
            return Result.Success(new BatchSearchResponse());
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, CancellationToken cancellationToken)
        {
            return Result.Success(new BatchSearchResponse());
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken)
        {
            return Result.Success(new BatchAttributesSearchResponse());
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken)
        {
            return Result.Success(new BatchAttributesSearchResponse());
        }

        public async Task<IResult<Stream>> DownloadFileAsync(string batchId, string filename)
        {
            return Result.Success<Stream>(new MemoryStream());
        }

        public async Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default)
        {
            return Result.Success(new DownloadFileResponse());
        }

        public async Task<IResult<IEnumerable<string>>> GetUserAttributesAsync()
        {
            return Result.Success<IEnumerable<string>>(new List<string>());
        }

        public async Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken)
        {
            return Result.Success<Stream>(new MemoryStream());
        }
    }
}

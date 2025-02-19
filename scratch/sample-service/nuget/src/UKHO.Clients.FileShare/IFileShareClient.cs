using UKHO.Clients.FileShare.Models;
using UKHO.Infrastructure.Results;

namespace UKHO.Clients.FileShare
{
    public interface IFileShareClient
    {
        Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(string batchId);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken);
        Task<IResult<Stream>> DownloadFileAsync(string batchId, string filename);
        Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default);
        Task<IResult<IEnumerable<string>>> GetUserAttributesAsync();
        Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken);
    }
}

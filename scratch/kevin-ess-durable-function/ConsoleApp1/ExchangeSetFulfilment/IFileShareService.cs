using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.FileShareService
{
    public interface IFileShareService
    {
        public Task<Object> CreateBatch(string userOid, string correlationId);
        Task<Object> GetBatchInfoBasedOnProducts(List<Object> products, Object message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath, string businessUnit);
        Task<bool> DownloadBatchFiles(Object entry, IEnumerable<string> uri, string downloadPath, Object queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken);
        Task<bool> DownloadReadMeFileFromFssAsync(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId);
        Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName);
        Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName);
        Task<bool> CommitAndGetBatchStatusForLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId);
        Task<IEnumerable<Object>> SearchFolderDetails(string batchId, string correlationId, string uri);
        Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<Object> fileDetails, string exchangeSetPath);
        Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = "zip");
        Task<string> SearchIhoPubFilePath(string batchId, string correlationId);
        Task<string> SearchIhoCrtFilePath(string batchId, string correlationId);
        Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string aioExchangeSetPath, string correlationId);
        Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string aioExchangeSetPath, string correlationId);
        Task<bool> DownloadReadMeFileFromCacheAsync(string batchId, string exchangeSetRootPath, string correlationId);
    }
}

using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public interface IFileShareReadWriteClient : IFileShareReadOnlyClient
    {
        Task<IResult<AppendAclResponse>> AppendAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default);

        Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, CancellationToken cancellationToken = default);

        Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, string correlationId, CancellationToken cancellationToken = default);

        Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(IBatchHandle batchHandle);

        Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, string correlationId,
            params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            string correlationId, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes);

        Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate, params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate,  string correlationId, CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken);
        Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, string correlationId, CancellationToken cancellationToken);
        Task<IResult<ReplaceAclResponse>> ReplaceAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default);
        Task<IResult> RollBackBatchAsync(IBatchHandle batchHandle);
        Task<IResult<RollBackBatchResponse>> RollBackBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken);
        Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default);
        Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry,
            string correlationId, CancellationToken cancellationToken = default);
    }
}

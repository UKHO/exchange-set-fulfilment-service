using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public interface IFileShareService
    {
        Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId);

        Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken);

        Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId);

        Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken = default(CancellationToken));
    }
}

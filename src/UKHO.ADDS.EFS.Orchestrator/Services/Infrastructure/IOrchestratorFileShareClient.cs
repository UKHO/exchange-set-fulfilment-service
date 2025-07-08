using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    internal interface IOrchestratorFileShareClient
    {
        /// <summary>
        ///     Creates a new batch in the File Share Service.
        /// </summary>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch handle on success or error information on failure.</returns>
        Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Commits a batch to the File Share Service.
        /// </summary>
        /// <param name="batchId">The batch identifier to commit.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the commit batch response on success or error information on failure.</returns>
        Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Searches for committed batches in the File Share Service, excluding the current batch.
        /// </summary>
        /// <param name="currentBatchId">The current batch identifier to exclude from results.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch search response on success or error information on failure.</returns>
        Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Sets the expiry date for multiple batches in the File Share Service.
        /// </summary>
        /// <param name="otherBatches">The list of batch details to set expiry dates for.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///     A result containing the last set expiry date response on success or error information on failure.
        ///     If no valid batches are found, returns a success result with an empty response.
        /// </returns>
        Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken);
    }
}

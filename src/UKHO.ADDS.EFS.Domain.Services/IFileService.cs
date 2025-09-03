using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.EFS.Domain.Files;

namespace UKHO.ADDS.EFS.Domain.Services
{
    public interface IFileService
    {
        /// <summary>
        ///     Creates a new batch.
        /// </summary>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch handle on success or error information on failure.</returns>
        Task<Batch> CreateBatchAsync(string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Commits a batch.
        /// </summary>
        /// <param name="batchId">The batch identifier to commit.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the commit batch response on success or error information on failure.</returns>
        Task<BatchCommit> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Searches for committed batches, excluding the current batch.
        /// </summary>
        /// <param name="currentBatchId">The current batch identifier to exclude from results.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch search response on success or error information on failure.</returns>
        Task<BatchSearchResponse> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Sets the expiry date for multiple batches.
        /// </summary>
        /// <param name="otherBatches">The list of batch details to set expiry dates for.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        ///     A result containing the last set expiry date response on success or error information on failure.
        ///     If no valid batches are found, returns a success result with an empty response.
        /// </returns>
        Task<bool> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken);

        /// <summary>
        ///     Adds a file to the specified batch.
        /// </summary>
        /// <param name="batchId">The batch identifier to add the file to.</param>
        /// <param name="fileStream">The stream containing the file data.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the add file to batch response on success or error information on failure.</returns>
        Task<AttributeList> AddFileToBatchAsync(string batchId, Stream fileStream, string fileName, string contentType, string correlationId, CancellationToken cancellationToken);
    }
}

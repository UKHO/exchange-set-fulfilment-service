using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Files;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Logging;
using UKHO.ADDS.EFS.Infrastructure.Logging.Services;
using UKHO.ADDS.Infrastructure.Results;
using Attribute = UKHO.ADDS.EFS.Domain.Files.Attribute;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    internal class DefaultFileService : IFileService
    {
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductCode = "S-100";
        private const string ProductCodeQueryClause = $"$batch(Product Code) eq '{ProductCode}'";
        private const int Limit = 100;
        private const int Start = 0;
        private const string SetExpiryDate = "SetExpiryDate";
        private const string CommitBatch = "CommitBatch";
        private const string CreateBatch = "CreateBatch";
        private const string AddFileToBatch = "AddFileToBatch";
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly ILogger<DefaultFileService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultFileService" /> class.
        /// </summary>
        /// <param name="fileShareReadWriteClient">The file share read-write client.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when fileShareReadWriteClient or logger is null.</exception>
        public DefaultFileService(IFileShareReadWriteClient fileShareReadWriteClient, ILogger<DefaultFileService> logger)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates a new batch in the File Share Service.
        /// </summary>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch handle on success or error information on failure.</returns>
        public async Task<Batch> CreateBatchAsync(CorrelationId correlationId, CancellationToken cancellationToken)
        {
            var createBatchResponseResult = await _fileShareReadWriteClient.CreateBatchAsync(GetBatchModel(), (string)correlationId, cancellationToken);

            if (createBatchResponseResult.IsFailure(out var error, out _))
            {
                LogFileShareServiceError(correlationId, CreateBatch, error, BatchId.None);
            }

            if (createBatchResponseResult.IsSuccess(out var response))
            {
                return new Batch()
                {
                    BatchId = BatchId.From(response.BatchId)
                };
            }

            throw new InvalidOperationException("Failed to create batch.");
        }

        /// <summary>
        ///     Commits a batch to the File Share Service.
        /// </summary>
        /// <param name="batchId">The batch identifier to commit.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The commit batch response on success or throws an exception on failure.</returns>
        public async Task<CommitBatchResponse> CommitBatchAsync(BatchHandle batchHandle, CorrelationId correlationId, CancellationToken cancellationToken)
        {
            var commitBatchResult = await _fileShareReadWriteClient.CommitBatchAsync(batchHandle, (string)correlationId, cancellationToken);

            if (commitBatchResult.IsFailure(out var commitError, out _))
            {
                LogFileShareServiceError(correlationId, CommitBatch, commitError, BatchId.From(batchHandle.BatchId));
                throw new InvalidOperationException("Failed to commit batch.");
            }

            if (commitBatchResult.IsSuccess(out var commitResponse))
            {
                return commitResponse;
            }

            throw new InvalidOperationException("Failed to commit batch.");
        }

        /// <summary>
        ///     Searches for committed batches in the File Share Service, excluding the current batch.
        /// </summary>
        /// <param name="currentBatchId">The current batch identifier to exclude from results.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch search response on success or error information on failure.</returns>
        public async Task<BatchSearchResponse> SearchCommittedBatchesExcludingCurrentAsync(BatchId currentBatchId, CorrelationId correlationId, CancellationToken cancellationToken)
        {
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductCodeQueryClause}";

            var searchResultResponse = await _fileShareReadWriteClient.SearchAsync(filter, Limit, Start, (string)correlationId, cancellationToken);

            if (searchResultResponse.IsFailure(out var error, out _))
            {
                LogSearchCommittedBatchesError(currentBatchId, correlationId, filter, Limit, Start, error);
            }

            if (searchResultResponse.IsSuccess(out var response))
            {
                if (response is { Entries: not null })
                {
                    response.Entries = response.Entries
                        .Where(e => !string.Equals(e.BatchId, (string)currentBatchId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                return response;
            }

            throw new InvalidOperationException("Failed to execute search.");
        }

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
        public async Task<bool> SetExpiryDateAsync(IEnumerable<BatchDetails> otherBatches, CorrelationId correlationId, CancellationToken cancellationToken)
        {
            // Filter valid batches before processing
            var validBatches = otherBatches.Where(b => !string.IsNullOrEmpty(b.BatchId)).ToList();

            if (validBatches.Count == 0)
            {
                return true;
            }

            var lastResult = true;

            foreach (var batch in validBatches)
            {
                var expiryResult = await _fileShareReadWriteClient.SetExpiryDateAsync(batch.BatchId, new BatchExpiryModel { ExpiryDate = DateTime.UtcNow.AddDays(2) }, (string)correlationId, cancellationToken);

                if (expiryResult.IsFailure(out var expiryError, out var expiry))
                {
                    LogFileShareServiceError(correlationId, SetExpiryDate, expiryError, BatchId.From(batch.BatchId));
                    return false;
                }

                lastResult = expiry.IsExpiryDateSet;
            }

            return lastResult;
        }

        /// <summary>
        ///     Adds a file to the specified batch in the File Share Service.
        /// </summary>
        /// <param name="batchId">The batch identifier to add the file to.</param>
        /// <param name="fileStream">The stream containing the file data.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the add file to batch response on success or error information on failure.</returns>
        public async Task<AttributeList> AddFileToBatchAsync(BatchHandle batchHandle, Stream fileStream, string fileName, string contentType, CorrelationId correlationId, CancellationToken cancellationToken)
        {
            var addFileResult = await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, fileStream, fileName, contentType, (string)correlationId, cancellationToken);

            if (addFileResult.IsFailure(out var error, out _))
            {
                LogFileShareServiceError(correlationId, AddFileToBatch, error, BatchId.From(batchHandle.BatchId));
                throw new InvalidOperationException("Failed to add file to batch.");
            }

            if (addFileResult.IsSuccess(out var response))
            {
                var attributeList = new AttributeList();
                if (response != null)
                {


                    foreach (var attribute in response.Attributes)
                    {
                        attributeList.Add(new Attribute { Key = attribute.Key, Value = attribute.Value });
                    }
                }
                return attributeList;
            }

            throw new InvalidOperationException("Failed to add file to batch.");
        }

        /// <summary>
        ///     Creates a batch model with predefined settings for S-100 product type.
        /// </summary>
        /// <returns>A configured batch model with appropriate access control and attributes.</returns>
        private static BatchModel GetBatchModel() =>
            new() { BusinessUnit = "ADDS-S100", Acl = new Acl { ReadUsers = new List<string> { "public" }, ReadGroups = new List<string> { "public" } }, Attributes = new List<KeyValuePair<string, string>> { new("Exchange Set Type", "Base"), new("Frequency", "DAILY"), new("Product Code", "S-100"), new("Media Type", "Zip") }, ExpiryDate = null };

        private void LogFileShareServiceError(CorrelationId correlationId, string endPoint, IError error, BatchId batchId)
        {
            var fileShareServiceLogView = new FileShareServiceLogView
            {
                BatchId = batchId,
                JobId = JobId.From((string)correlationId), // TODO Tidy - not correct
                EndPoint = endPoint,
                CorrelationId = correlationId,
                Error = error
            };

            _logger.LogFileShareError(fileShareServiceLogView);
        }

        private void LogSearchCommittedBatchesError(BatchId batchId, CorrelationId correlationId, string filter, int limit, int start, IError error)
        {
            var searchQuery = new SearchQueryLogView { Filter = filter, Limit = limit, Start = start };

            var searchCommittedBatchesLogView = new SearchCommittedBatchesLogView
            {
                BatchId = batchId,
                CorrelationId = correlationId,
                BusinessUnit = BusinessUnit,
                ProductCode = ProductCode,
                Query = searchQuery,
                Error = error
            };

            _logger.LogFileShareSearchCommittedBatchesError(searchCommittedBatchesLogView);
        }
    }
}

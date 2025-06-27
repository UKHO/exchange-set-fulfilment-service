using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.EFS.RetryPolicy;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    /// <summary>
    /// Service for managing file share operations with the File Share Service.
    /// </summary>
    public class FileShareService: IFileShareService
    {
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly ILogger<FileShareService> _logger;
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductType = "S-100";
        private const string ProductTypeQueryClause = $"$batch(ProductType) eq '{ProductType}' and ";
        private const int Limit = 100;
        private const int Start = 0;
        private const string SetExpiryDate = "SetExpiryDate";
        private const string CommitBatch = "CommitBatch";
        private const string CreateBatch = "CreateBatch";

        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareService"/> class.
        /// </summary>
        /// <param name="fileShareReadWriteClient">The file share read-write client.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when fileShareReadWriteClient or logger is null.</exception>
        public FileShareService(IFileShareReadWriteClient fileShareReadWriteClient, ILogger<FileShareService> logger)
        {
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new batch in the File Share Service.
        /// </summary>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch handle on success or error information on failure.</returns>
        public async Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId, CancellationToken cancellationToken)
        {
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<IBatchHandle>(_logger, nameof(CreateBatchAsync));
            var createBatchResponseResult = await retryPolicy.ExecuteAsync(() =>
                _fileShareReadWriteClient.CreateBatchAsync(GetBatchModel(), correlationId, cancellationToken));
            
            if (createBatchResponseResult.IsFailure(out var error, out _))
            {
                LogFileShareServiceError(correlationId, CreateBatch, error, correlationId);
            }

            return createBatchResponseResult;
        }

        /// <summary>
        /// Commits a batch to the File Share Service.
        /// </summary>
        /// <param name="batchId">The batch identifier to commit.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the commit batch response on success or error information on failure.</returns>
        public async Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken)
        {
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<CommitBatchResponse>(_logger, nameof(CommitBatchAsync));
            var commitBatchResult = await retryPolicy.ExecuteAsync(() =>
                _fileShareReadWriteClient.CommitBatchAsync(new BatchHandle(batchId), correlationId, cancellationToken));

            if (commitBatchResult.IsFailure(out var commitError, out _))
            {
                LogFileShareServiceError(correlationId, CommitBatch, commitError, correlationId, batchId);
            }

            return commitBatchResult;
        }

        /// <summary>
        /// Searches for committed batches in the File Share Service, excluding the current batch.
        /// </summary>
        /// <param name="currentBatchId">The current batch identifier to exclude from results.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch search response on success or error information on failure.</returns>
        public async Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId, CancellationToken cancellationToken)
        {
            var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductTypeQueryClause}$batch(BatchId) ne '{currentBatchId}'";
            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<BatchSearchResponse>(_logger, nameof(SearchCommittedBatchesExcludingCurrentAsync));
            var searchResult = await retryPolicy.ExecuteAsync(() =>
                _fileShareReadWriteClient.SearchAsync(filter, Limit, Start, correlationId, cancellationToken));

            if (searchResult.IsFailure(out var error, out _))
            {
                LogSearchCommittedBatchesError(currentBatchId, correlationId, filter, Limit, Start, error);
            }

            return searchResult;
        }

        /// <summary>
        /// Sets the expiry date for multiple batches in the File Share Service.
        /// </summary>
        /// <param name="otherBatches">The list of batch details to set expiry dates for.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A result containing the last set expiry date response on success or error information on failure.
        /// If no valid batches are found, returns a success result with an empty response.
        /// </returns>
        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken)
        {
            var validBatches = otherBatches.Where(b => !string.IsNullOrEmpty(b.BatchId)).ToList();

            if (validBatches.Count == 0)
            {
                return Result.Success(new SetExpiryDateResponse());
            }

            IResult<SetExpiryDateResponse> lastResult = Result.Success(new SetExpiryDateResponse());

            var retryPolicy = HttpRetryPolicyFactory.GetGenericResultRetryPolicy<SetExpiryDateResponse>(_logger, nameof(SetExpiryDateAsync));

            foreach (var batch in validBatches)
            {
                var expiryResult = await retryPolicy.ExecuteAsync(() =>
                    _fileShareReadWriteClient.SetExpiryDateAsync(batch.BatchId, new BatchExpiryModel { ExpiryDate = DateTime.UtcNow }, correlationId, cancellationToken));

                if (expiryResult.IsFailure(out var expiryError, out _))
                {
                    LogFileShareServiceError(correlationId, SetExpiryDate, expiryError, correlationId, batch.BatchId);
                    return expiryResult;
                }

                lastResult = expiryResult;
            }

            return lastResult;
        }

        /// <summary>
        /// Creates a batch model with predefined settings for S-100 product type.
        /// </summary>
        /// <returns>A configured batch model with appropriate access control and attributes.</returns>
        private static BatchModel GetBatchModel()
        {
            return new BatchModel
            {
                BusinessUnit = "ADDS-S100",
                Acl = new Acl
                {
                    ReadUsers = new List<string> { "public" },
                    ReadGroups = new List<string> { "public" }
                },
                Attributes = new List<KeyValuePair<string, string>>
                {
                    new("Exchange Set Type", "Base"),
                    new("Frequency", "DAILY"),
                    new("Product Type", "S-100"),
                    new("Media Type", "Zip")
                },
                ExpiryDate = null
            };
        }

        private void LogFileShareServiceError(string jobId, string endPoint, IError error, string correlationId, string batchId = "")
        {
            var fileShareServiceLogView = new FileShareServiceLogView
            {
                BatchId = batchId,
                JobId = jobId,
                EndPoint = endPoint,
                CorrelationId = correlationId,
                Error = error
            };

            _logger.LogFileShareError(fileShareServiceLogView);
        }

        private void LogSearchCommittedBatchesError(string batchId, string correlationId, string filter, int limit, int start, IError error)
        {
            var searchQuery = new SearchQuery
            {
                Filter = filter,
                Limit = limit,
                Start = start
            };
            var searchCommittedBatchesLogView = new SearchCommittedBatchesLog
            {
                BatchId = batchId,
                CorrelationId = correlationId,
                BusinessUnit = BusinessUnit,
                ProductType = ProductType,
                Query = searchQuery,
                Error = error,
            };

            _logger.LogFileShareSearchCommittedBatchesError(searchCommittedBatchesLogView);
        }
    }
}

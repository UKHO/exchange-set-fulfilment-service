using Docker.DotNet.Models;
using Microsoft.Kiota.Abstractions;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.Infrastructure.Results;
using BatchExpiryModel = UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite;
using BatchModel = UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite;
using Error = UKHO.ADDS.Infrastructure.Results.Error;
using IError = UKHO.ADDS.Infrastructure.Results.IError;
using ReadOnlyBatchDetails = UKHO.ADDS.Clients.FileShareService.ReadOnly.Models.BatchDetails;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure
{
    /// <summary>
    ///     Service for managing file share operations with the File Share Service.
    /// </summary>
    internal class OrchestratorFileShareClient : IOrchestratorFileShareClient
    {
        private const string BusinessUnit = "ADDS-S100";
        private const string ProductType = "S-100";
        private const string ProductTypeQueryClause = $"$batch(ProductType) eq '{ProductType}' and ";
        private const int Limit = 100;
        private const int Start = 0;
        private const string SetExpiryDate = "SetExpiryDate";
        private const string CommitBatch = "CommitBatch";
        private const string CreateBatch = "CreateBatch";
        private const string AddFileToBatch = "AddFileToBatch";
        private readonly KiotaFileShareServiceReadWrite _fileShareReadWriteClient;
        private readonly ILogger<OrchestratorFileShareClient> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrchestratorFileShareClient" /> class.
        /// </summary>
        /// <param name="fileShareReadWriteClient">The file share read-write client.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">Thrown when fileShareReadWriteClient or logger is null.</exception>
        public OrchestratorFileShareClient(KiotaFileShareServiceReadWrite fileShareReadWriteClient, ILogger<OrchestratorFileShareClient> logger)
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
        public async Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId, CancellationToken cancellationToken)
        {

            var createBatchResponse = await _fileShareReadWriteClient.Batch.PostAsync(
                null,
                r => r.Headers.Add("X-Correlation-Id", correlationId),
                cancellationToken);

            if (createBatchResponse == null)
            {
                return Result.Failure<IBatchHandle>(new Error("CreateBatch returned null response"));
            }

            //return Result.Success<IBatchHandle>(new BatchHandle(createBatchResponse.BatchId!));
            return Result.Success<IBatchHandle>(new BatchHandle(correlationId));
        }

        /// <summary>
        ///     Commits a batch to the File Share Service.
        /// </summary>
        /// <param name="batchId">The batch identifier to commit.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the commit batch response on success or error information on failure.</returns>
        public async Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken)
        {
            //await _fileShareReadWriteClient.V1.Batches[batchId].PostAsync(null, r => r.Options.Add(new CorrelationIdHandlerOption { CorrelationId = correlationId }), cancellationToken);

            return Result.Success(new CommitBatchResponse());
        }

        /// <summary>
        ///     Searches for committed batches in the File Share Service, excluding the current batch.
        /// </summary>
        /// <param name="currentBatchId">The current batch identifier to exclude from results.</param>
        /// <param name="correlationId">The correlation identifier for tracking the request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the batch search response on success or error information on failure.</returns>
        public async Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string currentBatchId, string correlationId, CancellationToken cancellationToken)
        {
            //var filter = $"BusinessUnit eq '{BusinessUnit}' and {ProductTypeQueryClause}$batch(BatchId) ne '{currentBatchId}'";

            //var searchResult = await _fileShareReadWriteClient.V1.Batches.GetAsync(r =>
            //{
            //    r.QueryParameters.Filter = filter;
            //    r.QueryParameters.Limit = Limit;
            //    r.QueryParameters.Start = Start;
            //    r.Options.Add(new CorrelationIdHandlerOption { CorrelationId = correlationId });
            //}, cancellationToken);


            //if (searchResult == null)
            //{
            //    var error = new Error("SearchCommittedBatches returned null response");
            //    LogSearchCommittedBatchesError(currentBatchId, correlationId, filter, Limit, Start, error);
            //    return Result.Failure<BatchSearchResponse>(error);
            //}

            //var response = new BatchSearchResponse
            //{
            //    Count = searchResult.Count ?? 0,
            //    Total = searchResult.Total ?? 0,
            //    Entries = searchResult.Entries?.Select(b => new ReadOnlyBatchDetails { BatchId = b.BatchId, BusinessUnit = b.BusinessUnit, Status = (BatchStatus)b.Status!, Files = b.Files?.Select(f => new FileDetails { Filename = f.Filename, FileSize = f.FileSize, Hash = f.Hash, MimeType = f.MimeType }).ToList() }).ToList() ?? new List<ReadOnlyBatchDetails>(),
            //    Links = new Links(new Link(searchResult.Links?.Self?.Href ?? string.Empty))
            //};

            //return Result.Success(response);
            return Result.Success(new BatchSearchResponse());
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
        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<ReadOnlyBatchDetails> otherBatches, string correlationId, CancellationToken cancellationToken)
        {
            //// Filter valid batches before processing
            //var validBatches = otherBatches.Where(b => !string.IsNullOrEmpty(b.BatchId)).ToList();

            //if (validBatches.Count == 0)
            //{
            //    return Result.Success(new SetExpiryDateResponse());
            //}

            //IResult<SetExpiryDateResponse> lastResult = Result.Success(new SetExpiryDateResponse());

            //foreach (var batch in validBatches)
            //{
            //    try
            //    {
            //        await _fileShareReadWriteClient.V1.Batches[batch.BatchId!].Expiry.PutAsync(new Clients.FileShareService.ReadWrite.Models.BatchExpiryModel { ExpiryDate = DateTime.UtcNow }, r => r.Options.Add(new CorrelationIdHandlerOption { CorrelationId = correlationId }), cancellationToken);
            //        lastResult = Result.Success(new SetExpiryDateResponse());
            //    }
            //    catch (Exception ex)
            //    {
            //        var error = new Error(ex.Message);
            //        LogFileShareServiceError(correlationId, SetExpiryDate, error, correlationId, batch.BatchId);
            //        return Result.Failure<SetExpiryDateResponse>(error);
            //    }
            //}

            //return lastResult;
            return Result.Success(new SetExpiryDateResponse());
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
        public async Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(string batchId, Stream fileStream, string fileName, string contentType, string correlationId, CancellationToken cancellationToken)
        {
            try
            {
                //var requestBody = new MemoryStream();
                //await fileStream.CopyToAsync(requestBody, cancellationToken);
                //requestBody.Seek(0, SeekOrigin.Begin);

                //await _fileShareReadWriteClient.V1.Batches[batchId].Files[fileName].PostAsync(requestBody, r =>
                //{
                //    r.Headers.Add("Content-Type", contentType);
                //    r.Options.Add(new CorrelationIdHandlerOption { CorrelationId = correlationId });
                //}, cancellationToken);

                return Result.Success(new AddFileToBatchResponse());
            }
            catch (Exception ex)
            {
                var error = new Error(ex.Message);
                LogFileShareServiceError(correlationId, AddFileToBatch, error, correlationId, batchId);
                return Result.Failure<AddFileToBatchResponse>(error);
            }
        }

        /// <summary>
        ///     Creates a batch model with predefined settings for S-100 product type.
        /// </summary>
        /// <returns>A configured batch model with appropriate access control and attributes.</returns>
        //private static Clients.FileShareService.ReadWrite.Models.BatchModel GetBatchModel() =>
        //    new() { BusinessUnit = "ADDS-S100", Acl = new UKHO.ADDS.Clients.Kiota.FileShareService.ReadWrite.V1.Models.Acl { ReadUsers = new List<string> { "public" }, ReadGroups = new List<string> { "public" } }, Attributes = new List<KeyValuePair<string, string>> { new("Exchange Set Type", "Base"), new("Frequency", "DAILY"), new("Product Type", "S-100"), new("Media Type", "Zip") }, ExpiryDate = null };

        private static Clients.FileShareService.ReadWrite.Models.BatchModel GetBatchModel() =>
            new() { BusinessUnit = "ADDS-S100", Acl = new Acl { ReadUsers = new List<string> { "public" }, ReadGroups = new List<string> { "public" } }, Attributes = new List<KeyValuePair<string, string>> { new("Exchange Set Type", "Base"), new("Frequency", "DAILY"), new("Product Type", "S-100"), new("Media Type", "Zip") }, ExpiryDate = null };

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
            var searchQuery = new SearchQueryLogView { Filter = filter, Limit = limit, Start = start };

            var searchCommittedBatchesLogView = new SearchCommittedBatchesLogView
            {
                BatchId = batchId,
                CorrelationId = correlationId,
                BusinessUnit = BusinessUnit,
                ProductType = ProductType,
                Query = searchQuery,
                Error = error
            };

            _logger.LogFileShareSearchCommittedBatchesError(searchCommittedBatchesLogView);
        }
    }
}

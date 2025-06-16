using System.Net;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class JobService
    {
        private readonly ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IFileShareService _fileShareService;
        private readonly ILogger<JobService> _logger;

        public JobService(ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, ISalesCatalogueService salesCatalogueService, ILogger<JobService> logger, IFileShareService fileShareService)
        {
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            _logger = logger;
            _fileShareService = fileShareService ?? throw new ArgumentNullException(nameof(fileShareService));
        }

        public async Task<ExchangeSetJob> CreateJob(ExchangeSetRequestQueueMessage queueMessage)
        {
            var job = await CreateJobEntity(queueMessage);

            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            await _timestampTable.CreateIfNotExistsAsync();

            var timestampResult = await _timestampTable.GetAsync(timestampKey, timestampKey);

            DateTime? timestamp = DateTime.MinValue;

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                timestamp = timestampEntity.Timestamp;
            }

            // Retrieve S100 products from the Sales Catalogue based on the timestamp
            await GetS100ProductsFromSpecificDateAsync(queueMessage, timestamp, job);

            // Create a batch for the job
            await ProcessCreateBatchAsync(queueMessage.CorrelationId, job);
            
            await _jobTable.CreateIfNotExistsAsync();
            await _jobTable.AddAsync(job);

            _logger.LogJobUpdated(ExchangeSetJobLogView.CreateFromJob(job));

            return job;
        }

        private async Task GetS100ProductsFromSpecificDateAsync(ExchangeSetRequestQueueMessage queueMessage, DateTime? timestamp, ExchangeSetJob job)
        {
            var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(timestamp, queueMessage);

            switch (result.s100SalesCatalogueData.ResponseCode)
            {
                case HttpStatusCode.OK when result.s100SalesCatalogueData.ResponseBody.Any():
                    // Products were successfully retrieved
                    job.Products = result.s100SalesCatalogueData.ResponseBody;
                    job.SalesCatalogueTimestamp = result.LastModified;
                    break;

                case HttpStatusCode.NotModified:
                    // No new data since the specified timestamp
                    job.State = ExchangeSetJobState.Cancelled;
                    break;

                default:
                    // Any other response code (error cases)
                    job.State = ExchangeSetJobState.Cancelled;
                    break;
            }
        }

        private async Task ProcessCreateBatchAsync(string correlationId, ExchangeSetJob job)
        {
            var createBatchResponseResult = await CreateBatchAsync(correlationId);

            if (createBatchResponseResult.IsSuccess(out var batchHandle, out _))
            {
                job.BatchId = batchHandle.BatchId;
                job.State = ExchangeSetJobState.InProgress;
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
            }
        }

        public async Task BuilderContainerCompletedAsync(long exitCode, ExchangeSetJob job)
        {
            if (exitCode != BuilderExitCodes.Success)
            {
                job.State = ExchangeSetJobState.Failed;
                await CompleteJobAsync(job);
                return;
            }

            await ProcessSuccessfulBuildAsync(job);
            await CompleteJobAsync(job);
        }

        private Task<ExchangeSetJob> CreateJobEntity(ExchangeSetRequestQueueMessage request)
        {
            var id = request.CorrelationId;

            var job = new ExchangeSetJob()
            {
                Id = id,
                DataStandard = request.DataStandard,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
                CorrelationId = request.CorrelationId
            };

            _logger.LogJobCreated(request.CorrelationId, ExchangeSetJobLogView.CreateFromJob(job));

            return Task.FromResult(job);
        }

        private async Task ProcessSuccessfulBuildAsync(ExchangeSetJob job)
        {
            // Try to commit the batch
            var commitBatchResult = await CommitBatchAsync(job.BatchId, job.CorrelationId);
            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return;
            }

            // Search for other committed batches
            var searchResult = await SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId);
            if (!searchResult.IsSuccess(out var searchResponse, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return;
            }

            // No previous batches found, mark as succeeded
            if (searchResponse?.Entries == null || searchResponse.Entries.Count == 0)
            {
                job.State = ExchangeSetJobState.Succeeded;
                return;
            }

            // Try to set expiry date on previous batches
            var expiryResult = await SetExpiryDateAsync(searchResponse.Entries, job.CorrelationId);
            job.State = expiryResult.IsSuccess(out _, out _)
                ? ExchangeSetJobState.Succeeded
                : ExchangeSetJobState.Failed;
        }

        private async Task CompleteJobAsync(ExchangeSetJob job)
        {
            await _jobTable.UpdateAsync(job);
            _logger.LogJobCompleted(ExchangeSetJobLogView.CreateFromJob(job));
        }

        private Task<IResult<IBatchHandle>> CreateBatchAsync(string correlationId, CancellationToken cancellationToken = default) =>
            _fileShareService.CreateBatchAsync(correlationId, cancellationToken);

        private Task<IResult<CommitBatchResponse>> CommitBatchAsync(string batchId, string correlationId, CancellationToken cancellationToken = default) =>
            _fileShareService.CommitBatchAsync(batchId, correlationId, cancellationToken);

        private Task<IResult<BatchSearchResponse>> SearchCommittedBatchesExcludingCurrentAsync(string batchId, string correlationId, CancellationToken cancellationToken = default) =>
            _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(batchId, correlationId, cancellationToken);

        private Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(List<BatchDetails> batchDetails, string correlationId, CancellationToken cancellationToken = default) =>
            _fileShareService.SetExpiryDateAsync(batchDetails, correlationId, cancellationToken);

    }
}

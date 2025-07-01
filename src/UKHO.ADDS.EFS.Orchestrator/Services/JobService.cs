using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews;
using UKHO.ADDS.EFS.Orchestrator.Services2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class JobService
    {
        private readonly S100ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly SalesCatalogueService _salesCatalogueService;
        private readonly FileShareService _fileShareService;
        private readonly ILogger<JobService> _logger;

        public JobService(S100ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, SalesCatalogueService salesCatalogueService, ILogger<JobService> logger, FileShareService fileShareService)
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

            var timestampResult = await _timestampTable.GetAsync(timestampKey, timestampKey);

            DateTime? timestamp = DateTime.MinValue;

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                timestamp = timestampEntity.Timestamp;
            }

            // Retrieve S100 products from the Sales Catalogue based on the timestamp
            await GetS100ProductsFromSpecificDateAsync(queueMessage, timestamp, job);

            // Create a batch for the job if it hasn't been cancelled
            if (job.State != ExchangeSetJobState.Cancelled)
            {
                await ProcessCreateBatchAsync(queueMessage.CorrelationId, job);
            }

            await _jobTable.AddAsync(job);

            //_logger.LogJobUpdated(ExchangeSetJobLogView.CreateFromJob(job));

            return job;
        }

        public async Task BuilderContainerCompletedAsync(long exitCode, ExchangeSetJob job)
        {
            //if (exitCode != BuilderExitCode.Success)
            //{
            //    job.State = ExchangeSetJobState.Failed;
            //    await CompleteJobAsync(job);
            //    return;
            //}

            await ProcessSuccessfulBuildAsync(job);
            await CompleteJobAsync(job);
        }

        private Task<S100ExchangeSetJob> CreateJobEntity(ExchangeSetRequestQueueMessage request)
        {
            var id = request.CorrelationId;

            var job = new S100ExchangeSetJob()
            {
                Id = id,
                DataStandard = request.DataStandard,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
                CorrelationId = request.CorrelationId
            };

            _logger.LogJobCreated(request.CorrelationId, ExchangeSetJobLogView.Create(job));

            return Task.FromResult(job);
        }

        private async Task GetS100ProductsFromSpecificDateAsync(ExchangeSetRequestQueueMessage queueMessage, DateTime? timestamp, S100ExchangeSetJob job)
        {
            //var result = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(timestamp, queueMessage);

            //switch (result.s100SalesCatalogueData.ResponseCode)
            //{
            //    case HttpStatusCode.OK when result.s100SalesCatalogueData.ResponseBody.Any():
            //        // Products were successfully retrieved
            //        job.Products = result.s100SalesCatalogueData.ResponseBody;
            //        job.SalesCatalogueTimestamp = result.LastModified;
            //        break;

            //    case HttpStatusCode.NotModified:
            //        // No new data since the specified timestamp
            //        job.State = ExchangeSetJobState.Cancelled;
            //        job.SalesCatalogueTimestamp = result.LastModified;
            //        break;

            //    default:
            //        // Any other response code (error cases)
            //        job.State = ExchangeSetJobState.Cancelled;
            //        job.SalesCatalogueTimestamp = timestamp;
            //        break;
            //}
        }

        private async Task ProcessCreateBatchAsync(string correlationId, ExchangeSetJob job)
        {
            var createBatchResponseResult = await _fileShareService.CreateBatchAsync(correlationId, CancellationToken.None);

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

        private async Task ProcessSuccessfulBuildAsync(ExchangeSetJob job)
        {
            // Try to commit the batch
            var commitBatchResult = await _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None);
            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return;
            }

            // Search for other committed batches
            var searchResult = await _fileShareService.SearchCommittedBatchesExcludingCurrentAsync(job.BatchId, job.CorrelationId, CancellationToken.None);
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
            var expiryResult = await _fileShareService.SetExpiryDateAsync(searchResponse.Entries, job.CorrelationId, CancellationToken.None);
            job.State = expiryResult.IsSuccess(out _, out _)
                ? ExchangeSetJobState.Succeeded
                : ExchangeSetJobState.Failed;
        }

        private async Task CompleteJobAsync(ExchangeSetJob job)
        {
            if (job.State == ExchangeSetJobState.Succeeded)
            {
                var updateTimestampEntity = new ExchangeSetTimestamp()
                {
                    DataStandard = job.DataStandard,
                    Timestamp = job.SalesCatalogueTimestamp
                };

                await _timestampTable.UpsertAsync(updateTimestampEntity);
            }

            //await _jobTable.UpdateAsync(job);
            //_logger.LogJobCompleted(ExchangeSetJobLogView.CreateFromJob(job));
        }
    }
}

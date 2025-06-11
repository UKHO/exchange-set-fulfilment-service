using System.Net;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Logging;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class JobService
    {
        private const string ScsApiVersion = "v2";
        private const string ProductType = "s100";
        private readonly ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IFileShareService _fileShareService;
        private readonly ILogger<JobService> _logger;

        public JobService(ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, ISalesCatalogueService salesCatalogueService, ILogger<JobService> logger, IFileShareService fileShareService)
        {
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _salesCatalogueService = salesCatalogueService;
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

            var (s100SalesCatalogueResponse, scsTimestamp) = await GetProductJson(timestamp, queueMessage);

            switch (s100SalesCatalogueResponse.ResponseCode)
            {
                case HttpStatusCode.OK when s100SalesCatalogueResponse.ResponseBody.Any():
                    
                await CreateBatchAsync(queueMessage, job);

                job.Products = s100SalesCatalogueResponse.ResponseBody;
                job.State = ExchangeSetJobState.InProgress;
                job.SalesCatalogueTimestamp = scsTimestamp;

                    break;
                case HttpStatusCode.NotModified:

                job.State = ExchangeSetJobState.ScsCatalogueUnchanged;
                    break;
                default:

                job.State = ExchangeSetJobState.Cancelled;
                    break;
            }

            job.SalesCatalogueTimestamp = scsTimestamp;

            await _jobTable.CreateIfNotExistsAsync();
            await _jobTable.AddAsync(job);

            _logger.LogJobUpdated(ExchangeSetJobLogView.CreateFromJob(job));

            return job;
        }

        public async Task BuilderContainerCompletedAsync(long exitCode, ExchangeSetJob job)
        {
            if (exitCode == BuilderExitCodes.Success)
            {
                if (await CommitBatchAsync(job))
                {
                    var batchDetails = await SearchAllCommitBatchesAsync(job);

                    if (batchDetails != null && job.State != ExchangeSetJobState.Failed && batchDetails.Count != 0)
                    {
                        if (await SetExpiryDateAsync(batchDetails, job))
                        {
                            job.State = ExchangeSetJobState.Succeeded;
                        }
                    }
                }
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
            }

            await _jobTable.UpdateAsync(job);
            _logger.LogJobCompleted(ExchangeSetJobLogView.CreateFromJob(job));
        }


        private async Task<(S100SalesCatalogueResponse s100SalesCatalogueResponse, DateTime? scsTimestamp)> GetProductJson(DateTime? timestamp, ExchangeSetRequestQueueMessage message)
        {
            var s100SalesCatalogueResult = await _salesCatalogueService.GetS100ProductsFromSpecificDateAsync(ScsApiVersion, ProductType, timestamp,
                    message);

            return s100SalesCatalogueResult;
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

        private async Task CreateBatchAsync(ExchangeSetRequestQueueMessage queueMessage, ExchangeSetJob job)
        {
            var createBatchResponseResult = await _fileShareService.CreateBatchAsync(queueMessage);
            if (createBatchResponseResult.IsSuccess(out var value, out var error))
            {
                job.BatchId = value.BatchId;
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
            }
        }

        private async Task<bool> CommitBatchAsync(ExchangeSetJob job)
        {
            var commitBatchResult = await _fileShareService.CommitBatchAsync(job.BatchId, job.CorrelationId, CancellationToken.None);

            if (commitBatchResult.IsFailure(out var commitError, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return false;
            }

            return true;
        }

        private async Task<List<BatchDetails>?> SearchAllCommitBatchesAsync(ExchangeSetJob job)
        {
            var searchResult = await _fileShareService.SearchAllCommitBatchesAsync(job.BatchId, job.CorrelationId);

            if (!searchResult.IsSuccess(out var value, out var error))
            {
                job.State = ExchangeSetJobState.Failed;
            }
            else if (value?.Entries == null || value.Entries.Count == 0)
            {
                job.State = ExchangeSetJobState.Succeeded;
            }

            return value?.Entries;
        }

        private async Task<bool> SetExpiryDateAsync(List<BatchDetails> batchDetails, ExchangeSetJob job)
        {
            var expiryResult = await _fileShareService.SetExpiryDateAsync(batchDetails, job.CorrelationId, CancellationToken.None);

            if (expiryResult.IsFailure(out var expiryError, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return false;
            }
               
            return true;
        }
    }
}

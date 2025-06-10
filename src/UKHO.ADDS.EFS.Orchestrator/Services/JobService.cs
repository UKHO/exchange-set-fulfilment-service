using System.Net;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
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
        private const string ScsApiVersion = "v2";
        private const string ProductType = "s100";
        private readonly ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly IFileShareReadWriteClient _fileShareReadWriteClient;
        private readonly ILogger<JobService> _logger;

        public JobService(ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, ISalesCatalogueClient salesCatalogueClient, ILogger<JobService> logger, IFileShareReadWriteClient fileShareReadWriteClient)
        {
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _salesCatalogueClient = salesCatalogueClient;
            _logger = logger;
            _fileShareReadWriteClient = fileShareReadWriteClient ?? throw new ArgumentNullException(nameof(fileShareReadWriteClient));
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

                //Call FSS Create Batch Return batchID
                var createBatchResponseResult = await CreateBatchAsync(queueMessage);

                if (createBatchResponseResult.IsSuccess(out var value, out var error))
                {
                    job.BatchId = value.BatchId;
                }

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
                    await SetExpiryDateAsync(job);
                    job.State = ExchangeSetJobState.Succeeded;
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
            var s100SalesCatalogueResult = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(ScsApiVersion, ProductType, timestamp, message.CorrelationId);

            if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            {
                switch (s100SalesCatalogueData.ResponseCode)
                {
                    case HttpStatusCode.OK:
                        return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

                    case HttpStatusCode.NotModified:
                        return (s100SalesCatalogueData, timestamp);
                }
            }
            else
            {
                _logger.LogSalesCatalogueError(error, message);
            }

            return (new S100SalesCatalogueResponse(), timestamp);
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

        private async Task<IResult<IBatchHandle>> CreateBatchAsync(ExchangeSetRequestQueueMessage queueMessage)
        {
            var createBatchResponseResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel
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
            }, queueMessage.CorrelationId);

            return createBatchResponseResult;
        }

        private async Task<bool> CommitBatchAsync(ExchangeSetJob job)
        {
            var commitBatchResult = await _fileShareReadWriteClient.CommitBatchAsync(
                new BatchHandle(job.BatchId), job.CorrelationId, CancellationToken.None);

            if (commitBatchResult.IsFailure(out var commitError, out _))
            {
                job.State = ExchangeSetJobState.Failed;
                return false;
            }

            return true;
        }

        public async Task<List<BatchDetails>> GetAllBatchesExceptCurrentAsync(
            string currentBatchId,
            string correlationId,
            IFileShareReadOnlyClient readOnlyClient)
        {
            var filter =
                $"BusinessUnit eq 'ADDS-S100' and " +
                $"$batch(ProductType) eq 'S-100' and " +
                $"$batch(BatchId) ne '{currentBatchId}'";

            var limit = 100;
            var start = 0;
            var allBatches = new List<BatchDetails>();

            while (true)
            {
                var searchResult = await readOnlyClient.SearchAsync(filter, limit, start, correlationId);
                if (!searchResult.IsSuccess(out var value, out var error))
                {
                    // Handle error as needed
                    break;
                }

                if (value.Entries != null)
                    allBatches.AddRange(value.Entries);

                var next = value.Links?.Next?.Href;
                if (string.IsNullOrEmpty(next)) break;

                var queryParams = System.Web.HttpUtility.ParseQueryString(new Uri(next).Query);
                start = int.TryParse(queryParams["start"], out var s) ? s : start + limit;
            }

            return allBatches;
        }
        private async Task SetExpiryDateAsync(ExchangeSetJob job)
        {
            // Example usage of GetAllBatchesExceptCurrentAsync
            // You need to provide the currentBatchId, correlationId, and a readOnlyClient instance
            // Replace the following placeholders with actual values as needed
            var currentBatchId = job.BatchId;
            var correlationId = job.CorrelationId;

            var otherBatches = await GetAllBatchesExceptCurrentAsync(currentBatchId, correlationId, _fileShareReadWriteClient);

            foreach (var batch in otherBatches)
            {
                if (!string.IsNullOrEmpty(batch.BatchId))
                {
                    var expiryResult = await _fileShareReadWriteClient.SetExpiryDateAsync(
                        batch.BatchId,
                        new BatchExpiryModel { ExpiryDate = DateTime.UtcNow },
                        job.CorrelationId,
                        CancellationToken.None);

                    if (expiryResult.IsFailure(out var expiryError, out _))
                    {
                        // _logger.LogError($"Failed to set expiry date for batch {batch.BatchId}: {expiryError}");
                        // Optionally handle the error as needed
                    }
                }
            }
        }
    }
}

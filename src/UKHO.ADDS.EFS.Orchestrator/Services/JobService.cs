using System.Net;
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
                //call fss commit batch

                //call fss expiry endpoint

                var updateTimestampEntity = new ExchangeSetTimestamp()
                {
                    DataStandard = job.DataStandard,
                    Timestamp = job.SalesCatalogueTimestamp,
                    BatchId = job.BatchId,
                };

                await _timestampTable.UpsertAsync(updateTimestampEntity);

                job.State = ExchangeSetJobState.Succeeded;
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
    }
}

using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
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
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<JobService> _logger;

        public JobService(ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, ISalesCatalogueClient salesCatalogueClient, ILogger<JobService> logger)
        {
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _salesCatalogueClient = salesCatalogueClient;
            _logger = logger;
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

                    job.Products = s100SalesCatalogueResponse.ResponseBody;
                    job.State = ExchangeSetJobState.InProgress;
                    job.SalesCatalogueTimestamp = scsTimestamp;
                    job.BatchId = timestampEntity?.BatchId;  // this is the batch ID from the timestamp table, which is used to track the previous batchid
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
                    Timestamp = job.SalesCatalogueTimestamp
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
    }
}

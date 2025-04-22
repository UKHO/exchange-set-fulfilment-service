using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class JobService
    {
        private const string ScsApiVersion = "v2";
        private const string ProductType = "s100";
        private readonly string _salesCatalogueServiceEndpoint;
        private readonly ExchangeSetJobTable _jobTable;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly ISalesCatalogueClient _salesCatalogueClient;
        private readonly ILogger<JobService> _logger;

        public JobService(string salesCatalogueServiceEndpoint, ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, ISalesCatalogueClient salesCatalogueClient, ILogger<JobService> logger)
        {
            _salesCatalogueServiceEndpoint = salesCatalogueServiceEndpoint;
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _salesCatalogueClient = salesCatalogueClient;
            _logger = logger;
        }

        public async Task<ExchangeSetJob> CreateJob(ExchangeSetRequestMessage request)
        {
            _logger.LogInformation("Create Job started with DataStandard: {DataStandard} | Correlation ID: {_X-Correlation-ID}", request.DataStandard, request.CorrelationId);

            var job = await CreateJobEntity(request);
            _logger.LogInformation("Job entity created with Id: {JobId}", job.Id);

            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            await _timestampTable.CreateIfNotExistsAsync();

            var timestampResult = await _timestampTable.GetAsync(timestampKey, timestampKey);

            DateTime? timestamp = DateTime.MinValue;

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                timestamp = timestampEntity.Timestamp;
            }

            var productInfo = await GetProductJson(timestamp, request.CorrelationId);

            if (productInfo.s100SalesCatalogueResponse.ResponseCode == HttpStatusCode.OK && productInfo.s100SalesCatalogueResponse.ResponseBody.Any())
            {
                job.Products = productInfo.s100SalesCatalogueResponse.ResponseBody;
                job.State = ExchangeSetJobState.InProgress;
                job.SalesCatalogueTimestamp = productInfo.scsTimestamp;

                await _jobTable.CreateIfNotExistsAsync();

                await _jobTable.AddAsync(job);

                _logger.LogInformation("Job state set to InProgress with {ProductCount} products. | Correlation ID: {_X-Correlation-ID}", productInfo.s100SalesCatalogueResponse.ResponseBody.Count, request.CorrelationId);
                _logger.LogInformation("Job added to the table with Id: {JobId} | Correlation ID: {_X-Correlation-ID}", job.Id, request.CorrelationId);
            }
            else if (productInfo.s100SalesCatalogueResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                job.State = ExchangeSetJobState.ScsCatalogueUnchanged;
                _logger.LogWarning("Job {job.Id} skipped as SCS catalogue is unchanged. State: {job.State} | Correlation ID: {_X-Correlation-ID}", job.Id, job.State, job.CorrelationId);
            }
            else
            {
                job.State = ExchangeSetJobState.Cancelled;
                _logger.LogWarning("Job {job.Id} skipped as SCS catalogue is cancelled. State: {job.State} | Correlation ID: {_X-Correlation-ID}", job.Id, job.State, job.CorrelationId);
            }

            job.SalesCatalogueTimestamp = productInfo.scsTimestamp;

            _logger.LogInformation("Create Job has completed with DataStandard: {DataStandard}. | Correlation ID: {_X-Correlation-ID}", request.DataStandard, request.CorrelationId);

            return job;
        }

        public async Task CompleteJobAsync(long exitCode, ExchangeSetJob job)
        {
            // This should be the success path - set to 'Failed' to demonstrate writing the timestamp
            // All jobs currently 'fail', because the builder reports that a number of pipeline stages return 'NotRun'
            if (exitCode == BuilderExitCodes.Failed)
            {
                var updateTimestampEntity = new ExchangeSetTimestamp()
                {
                    DataStandard = job.DataStandard,
                    Timestamp = job.SalesCatalogueTimestamp
                };

                await _timestampTable.UpsertAsync(updateTimestampEntity);

                job.State = ExchangeSetJobState.Succeeded;

                _logger.LogInformation("Job {job.Id} was completed. State: {job.State} | Correlation ID: {_X-Correlation-ID}", job.Id, job.State, job.CorrelationId);
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
                _logger.LogInformation("Job {job.Id} was completed. State: {job.State} | Correlation ID: {_X-Correlation-ID}", job.Id, job.State, job.CorrelationId);
            }

            await _jobTable.UpdateAsync(job);
        }

        private async Task<(S100SalesCatalogueResponse s100SalesCatalogueResponse, DateTime? scsTimestamp)> GetProductJson(DateTime? timestamp, string correlationId)
        {
            _logger.LogInformation("Starting GetProductJson with timestamp: {Timestamp} | Correlation ID: {_X-Correlation-ID}", timestamp, correlationId);

            var timestampString = (timestamp.HasValue && timestamp.Value == DateTime.MinValue) ? string.Empty : timestamp?.ToString("R");

            var s100SalesCatalogueResult = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(ScsApiVersion, ProductType, timestampString, correlationId);

            if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            {
                _logger.LogInformation("Successfully retrieved data from Sales Catalogue Service with response code: {ResponseCode} | Correlation ID: {_X-Correlation-ID}", s100SalesCatalogueData!.ResponseCode, correlationId);

                switch (s100SalesCatalogueData.ResponseCode)
                {
                    case HttpStatusCode.OK:
                        _logger.LogInformation("Sales Catalogue Service returned OK with {ProductCount} products. | Correlation ID: {_X-Correlation-ID}", s100SalesCatalogueData.ResponseBody.Count, correlationId);
                        return (s100SalesCatalogueData, s100SalesCatalogueData.LastModified);

                    case HttpStatusCode.NotModified:
                        _logger.LogInformation("Sales Catalogue Service returned NotModified. Using existing timestamp: {Timestamp} | Correlation ID: {_X-Correlation-ID}", timestamp, correlationId);
                        return (s100SalesCatalogueData, timestamp);
                }
            }
            else
            {
                var errorMessage = string.IsNullOrEmpty(error?.Message) ? error?.Metadata["ErrorResponse"] : error?.Message;
                _logger.LogWarning("Failed to retrieve S100 products from Sales Catalogue Service. Error: {Error} | Correlation ID: {_X-Correlation-ID}", errorMessage, correlationId);
            }

            _logger.LogWarning("Returning empty product list and timestamp due to failure. | Correlation ID: {_X-Correlation-ID}", correlationId);
            return (new S100SalesCatalogueResponse(), timestamp);
        }

        private Task<ExchangeSetJob> CreateJobEntity(ExchangeSetRequestMessage request)
        {
            var id = Guid.NewGuid().ToString("N");

            var job = new ExchangeSetJob()
            {
                Id = id,
                DataStandard = request.DataStandard,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
                CorrelationId = request.CorrelationId
            };

            return Task.FromResult(job);
        }
    }
}

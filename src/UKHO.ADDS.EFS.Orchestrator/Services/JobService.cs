using System.Net;
using UKHO.ADDS.Clients.SalesCatalogueService;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class JobService
    {
        private const string SCSApiVersion = "v2";
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
            _logger.LogInformation("Create Job has started with CorrelationId: {CorrelationId} and DataStandard: {DataStandard}", request.CorrelationId, request.DataStandard);

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

            if (productInfo.s100Products.Any())
            {
                job.Products = productInfo.s100Products;
                job.State = ExchangeSetJobState.InProgress;
                _logger.LogInformation("Job state set to InProgress with {ProductCount} products.", productInfo.s100Products.Count);
            }
            else
            {
                job.State = ExchangeSetJobState.Cancelled;
                _logger.LogInformation("No products found. Job state set to Cancelled.");
            }

            job.SalesCatalogueTimestamp = productInfo.scsTimestamp;

            await _jobTable.CreateIfNotExistsAsync();

            await _jobTable.AddAsync(job);
            _logger.LogInformation("Job added to the table with Id: {JobId}", job.Id);

            _logger.LogInformation("Create Job has completed for CorrelationId: {CorrelationId}", request.CorrelationId);

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
            }
            else
            {
                job.State = ExchangeSetJobState.Failed;
            }

            await _jobTable.UpdateAsync(job);
        }

        private async Task<(List<S100Products> s100Products, DateTime? scsTimestamp)> GetProductJson(DateTime? timestamp, string correlationId)
        {
            _logger.LogInformation("Starting GetProductJson with timestamp: {Timestamp} and correlationId: {CorrelationId}", timestamp, correlationId);

            var timestampString = (timestamp.HasValue && timestamp.Value == DateTime.MinValue) ? string.Empty : timestamp?.ToString("R");

            var s100SalesCatalogueResult = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(SCSApiVersion, ProductType, timestampString!, correlationId);

            if (s100SalesCatalogueResult.IsSuccess(out var s100SalesCatalogueData, out var error))
            {
                _logger.LogInformation("Successfully retrieved data from Sales Catalogue Service with response code: {ResponseCode}", s100SalesCatalogueData!.ResponseCode);

                switch (s100SalesCatalogueData.ResponseCode)
                {
                    case HttpStatusCode.OK:
                        _logger.LogInformation("Sales Catalogue Service returned OK with {ProductCount} products", s100SalesCatalogueData.ResponseBody.Count);
                        return (s100SalesCatalogueData.ResponseBody, s100SalesCatalogueData.LastModified);

                    case HttpStatusCode.NotModified:
                        _logger.LogInformation("Sales Catalogue Service returned NotModified. Using existing timestamp: {Timestamp}", timestamp);
                        return (new List<S100Products>(), timestamp);
                }
            }
            else
            {
                _logger.LogError("Failed to retrieve S100 products from Sales Catalogue Service. Error: {Error}", error?.Message);
            }

            _logger.LogError("Returning empty product list and timestamp due to failure.");
            return (new List<S100Products>(), timestamp);
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

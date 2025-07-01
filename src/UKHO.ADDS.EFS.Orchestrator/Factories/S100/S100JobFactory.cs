using System.Net;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Builders.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews;
using UKHO.ADDS.EFS.Orchestrator.Services2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Tables;
using UKHO.ADDS.EFS.Orchestrator.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Factories.S100
{
    internal class S100JobFactory : JobFactory<S100ExchangeSetJob>
    {
        private readonly S100BuildRequestProcessor _buildRequestProcessor;
        private readonly ExchangeSetTimestampTable _timestampTable;
        private readonly S100ExchangeSetJobTable _jobTable;
        private readonly BuildStatusTable _statusTable;
        private readonly SalesCatalogueService _salesCatalogueClient;
        private readonly FileShareService _fileShareClient;
        private readonly ILogger<S100JobFactory> _logger;

        public S100JobFactory(S100BuildRequestProcessor buildRequestProcessor, ExchangeSetTimestampTable timestampTable, S100ExchangeSetJobTable jobTable, BuildStatusTable statusTable, SalesCatalogueService salesCatalogueClient, FileShareService fileShareClient, ILogger<S100JobFactory> logger)
        {
            _buildRequestProcessor = buildRequestProcessor;
            _timestampTable = timestampTable;
            _jobTable = jobTable;
            _statusTable = statusTable;
            _salesCatalogueClient = salesCatalogueClient;
            _fileShareClient = fileShareClient;
            _logger = logger;
        }

        public override async Task CreateJobAsync(S100ExchangeSetJob job, CancellationToken stoppingToken)
        {
            var timestamp = await _timestampTable.GetTimestampForJob(job);

            // Retrieve S100 products from the Sales Catalogue based on the timestamp
            await GetS100ProductsFromSpecificDateAsync(job, timestamp);

            // Create a batch for the job if it hasn't been cancelled
            if (job.State != ExchangeSetJobState.Cancelled)
            {
                await ProcessCreateBatchAsync(job, stoppingToken);
            }

            await _jobTable.AddAsync(job);
            await _statusTable.AddAsync(new BuildStatus() { DataStandard = job.DataStandard, ExitCode = BuilderExitCode.NotRun, JobId = job.Id});

            _logger.LogJobUpdated(ExchangeSetJobLogView.Create(job));

            await _buildRequestProcessor.SendBuildRequestAsync(job, stoppingToken);
        }

        private async Task GetS100ProductsFromSpecificDateAsync(S100ExchangeSetJob job, DateTime? timestamp)
        {
            var result = await _salesCatalogueClient.GetS100ProductsFromSpecificDateAsync(timestamp, job);

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
                    job.SalesCatalogueTimestamp = result.LastModified;
                    break;

                default:
                    // Any other response code (error cases)
                    job.State = ExchangeSetJobState.Cancelled;
                    job.SalesCatalogueTimestamp = timestamp;
                    break;
            }
        }

        private async Task ProcessCreateBatchAsync(ExchangeSetJob job, CancellationToken stoppingToken)
        {
            var createBatchResponseResult = await _fileShareClient.CreateBatchAsync(job.CorrelationId, stoppingToken);

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
    }
}

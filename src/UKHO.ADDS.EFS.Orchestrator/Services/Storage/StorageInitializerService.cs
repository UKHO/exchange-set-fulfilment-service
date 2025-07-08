using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Storage
{
    internal class StorageInitializerService
    {
        private readonly QueueServiceClient _queueClient;

        private readonly ITable<BuildStatus> _buildStatusTable;
        private readonly ITable<S100ExchangeSetJob> _s100JobTable;
        private readonly ITable<S63ExchangeSetJob> _s63JobTable;
        private readonly ITable<S57ExchangeSetJob> _s57JobTable;
        private readonly ITable<S100BuildSummary> _s100BuildSummaryTable;
        private readonly ITable<S63BuildSummary> _s63BuildSummaryTable;
        private readonly ITable<S57BuildSummary> _s57BuildSummaryTable;
        private readonly ITable<ExchangeSetJobType> _jobTypeTable;
        private readonly ITable<ExchangeSetTimestamp> _timestampTable;

        public StorageInitializerService
            (
                QueueServiceClient queueClient,
                ITable<ExchangeSetJobType> jobTypeTable,
                ITable<S100ExchangeSetJob> s100JobTable,
                ITable<S63ExchangeSetJob> s63JobTable,
                ITable<S57ExchangeSetJob> s57JobTable,
                ITable<S100BuildSummary> s100BuildSummaryTable,
                ITable<S63BuildSummary> s63BuildSummaryTable,
                ITable<S57BuildSummary> s57BuildSummaryTable,
                ITable<ExchangeSetTimestamp> timestampTable,
                ITable<BuildStatus> buildStatusTable)
        {
            _queueClient = queueClient;
            _jobTypeTable = jobTypeTable;
            _s100JobTable = s100JobTable;
            _s63JobTable = s63JobTable;
            _s57JobTable = s57JobTable;
            _s100BuildSummaryTable = s100BuildSummaryTable;
            _s63BuildSummaryTable = s63BuildSummaryTable;
            _s57BuildSummaryTable = s57BuildSummaryTable;
            _timestampTable = timestampTable;
            _buildStatusTable = buildStatusTable;
        }

        public async Task InitializeStorageAsync(CancellationToken stoppingToken)
        {
            try
            {
                await EnsureQueueExists(StorageConfiguration.JobRequestQueueName, stoppingToken);

                await EnsureQueueExists(StorageConfiguration.S100BuildRequestQueueName, stoppingToken);
                await EnsureQueueExists(StorageConfiguration.S100BuildResponseQueueName, stoppingToken);

                await EnsureQueueExists(StorageConfiguration.S63BuildRequestQueueName, stoppingToken);
                await EnsureQueueExists(StorageConfiguration.S63BuildResponseQueueName, stoppingToken);

                await EnsureQueueExists(StorageConfiguration.S57BuildRequestQueueName, stoppingToken);
                await EnsureQueueExists(StorageConfiguration.S57BuildResponseQueueName, stoppingToken);

                await _s100JobTable.CreateIfNotExistsAsync(stoppingToken);
                await _s63JobTable.CreateIfNotExistsAsync(stoppingToken);
                await _s57JobTable.CreateIfNotExistsAsync(stoppingToken);

                await _s100BuildSummaryTable.CreateIfNotExistsAsync(stoppingToken);
                await _s63BuildSummaryTable.CreateIfNotExistsAsync(stoppingToken);
                await _s57BuildSummaryTable.CreateIfNotExistsAsync(stoppingToken);

                await _timestampTable.CreateIfNotExistsAsync(stoppingToken);
                await _jobTypeTable.CreateIfNotExistsAsync(stoppingToken);
                await _buildStatusTable.CreateIfNotExistsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new InvalidOperationException("Failed to initialize storage", ex);
            }
        }

        private async Task EnsureQueueExists(string queueName, CancellationToken stoppingToken)
        {
            var jobRequestQueue = _queueClient.GetQueueClient(queueName);
            await jobRequestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
        }
    }
}

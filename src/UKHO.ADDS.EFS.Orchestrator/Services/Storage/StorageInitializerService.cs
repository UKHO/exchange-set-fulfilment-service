using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Storage
{
    internal class StorageInitializerService
    {
        private readonly BuildStatusTable _buildStatusTable;
        private readonly S100ExchangeSetJobTable _s100JobTable;
        private readonly S63ExchangeSetJobTable _s63JobTable;
        private readonly S57ExchangeSetJobTable _s57JobTable;
        private readonly S100BuildSummaryTable _s100BuildSummaryTable;
        private readonly S63BuildSummaryTable _s63BuildSummaryTable;
        private readonly S57BuildSummaryTable _s57BuildSummaryTable;
        private readonly QueueServiceClient _queueClient;
        private readonly ExchangeSetJobTypeTable _jobTypeTable;
        private readonly ExchangeSetTimestampTable _timestampTable;

        public StorageInitializerService
            (
                QueueServiceClient queueClient,
                ExchangeSetJobTypeTable jobTypeTable,
                S100ExchangeSetJobTable s100JobTable,
                S63ExchangeSetJobTable s63JobTable,
                S57ExchangeSetJobTable s57JobTable,
                S100BuildSummaryTable s100BuildSummaryTable,
                S63BuildSummaryTable s63BuildSummaryTable,
                S57BuildSummaryTable s57BuildSummaryTable,
                ExchangeSetTimestampTable timestampTable,
                BuildStatusTable buildStatusTable)
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

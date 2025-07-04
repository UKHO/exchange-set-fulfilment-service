using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Services2.Storage
{
    internal class StorageInitializerService
    {
        private readonly BuildStatusTable _buildStatusTable;
        private readonly S100ExchangeSetJobTable _jobTable;
        private readonly QueueServiceClient _queueClient;
        private readonly ExchangeSetTimestampTable _timestampTable;

        public StorageInitializerService(QueueServiceClient queueClient, S100ExchangeSetJobTable jobTable, ExchangeSetTimestampTable timestampTable, BuildStatusTable buildStatusTable)
        {
            _queueClient = queueClient;
            _jobTable = jobTable;
            _timestampTable = timestampTable;
            _buildStatusTable = buildStatusTable;
        }

        public async Task InitializeStorageAsync(CancellationToken stoppingToken)
        {
            try
            {
                var jobRequestQueue = _queueClient.GetQueueClient(StorageConfiguration.JobRequestQueueName);
                await jobRequestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

                var s100BuildRequestQueue = _queueClient.GetQueueClient(StorageConfiguration.S100BuildRequestQueueName);
                await s100BuildRequestQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

                var s100BuildResponseQueue = _queueClient.GetQueueClient(StorageConfiguration.S100BuildResponseQueueName);
                await s100BuildResponseQueue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

                await _jobTable.CreateIfNotExistsAsync(stoppingToken);
                await _timestampTable.CreateIfNotExistsAsync(stoppingToken);
                await _buildStatusTable.CreateIfNotExistsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new InvalidOperationException("Failed to initialize storage", ex);
            }
        }
    }
}

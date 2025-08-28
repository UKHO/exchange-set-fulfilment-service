using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Storage
{
    internal class StorageInitializerService
    {
        private readonly QueueServiceClient _queueClient;

        private readonly ITable<DataStandardTimestamp> _timestampTable;
        private readonly ITable<Job> _jobTable;
        private readonly ITable<S100Build> _s100BuildTable;
        private readonly ITable<S63Build> _s63BuildTable;
        private readonly ITable<S57Build> _s57BuildTable;
        private readonly ITable<BuildMemento> _buildMementoTable;

        public StorageInitializerService
            (
                QueueServiceClient queueClient,
                ITable<DataStandardTimestamp> timestampTable,
                ITable<Job> jobTable,
                ITable<S100Build> s100BuildTable,
                ITable<S63Build> s63BuildTable,
                ITable<S57Build> s57BuildTable,
                ITable<BuildMemento> buildMementoTable)
        {
            _queueClient = queueClient;
            _timestampTable = timestampTable;
            _jobTable = jobTable;
            _s100BuildTable = s100BuildTable;
            _s63BuildTable = s63BuildTable;
            _s57BuildTable = s57BuildTable;
            _buildMementoTable = buildMementoTable;
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

                await _s100BuildTable.CreateIfNotExistsAsync(stoppingToken);
                await _s63BuildTable.CreateIfNotExistsAsync(stoppingToken);
                await _s57BuildTable.CreateIfNotExistsAsync(stoppingToken);

                await _jobTable.CreateIfNotExistsAsync(stoppingToken);

                await _timestampTable.CreateIfNotExistsAsync(stoppingToken);
                await _buildMementoTable.CreateIfNotExistsAsync(stoppingToken);
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

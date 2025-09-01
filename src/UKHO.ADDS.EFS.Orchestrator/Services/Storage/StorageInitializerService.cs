using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Storage
{
    internal class StorageInitializerService
    {
        private readonly QueueServiceClient _queueClient;

        private readonly IRepository<DataStandardTimestamp> _timestampRepository;
        private readonly IRepository<Job> _jobRepository;
        private readonly IRepository<S100Build> _s100BuildRepository;
        private readonly IRepository<S63Build> _s63BuildRepository;
        private readonly IRepository<S57Build> _s57BuildRepository;
        private readonly IRepository<BuildMemento> _buildMementoRepository;

        public StorageInitializerService
            (
                QueueServiceClient queueClient,
                IRepository<DataStandardTimestamp> timestampRepository,
                IRepository<Job> jobRepository,
                IRepository<S100Build> s100BuildRepository,
                IRepository<S63Build> s63BuildRepository,
                IRepository<S57Build> s57BuildRepository,
                IRepository<BuildMemento> buildMementoRepository)
        {
            _queueClient = queueClient;
            _timestampRepository = timestampRepository;
            _jobRepository = jobRepository;
            _s100BuildRepository = s100BuildRepository;
            _s63BuildRepository = s63BuildRepository;
            _s57BuildRepository = s57BuildRepository;
            _buildMementoRepository = buildMementoRepository;
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

                await _s100BuildRepository.CreateIfNotExistsAsync(stoppingToken);
                await _s63BuildRepository.CreateIfNotExistsAsync(stoppingToken);
                await _s57BuildRepository.CreateIfNotExistsAsync(stoppingToken);

                await _jobRepository.CreateIfNotExistsAsync(stoppingToken);

                await _timestampRepository.CreateIfNotExistsAsync(stoppingToken);
                await _buildMementoRepository.CreateIfNotExistsAsync(stoppingToken);
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

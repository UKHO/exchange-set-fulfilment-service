using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S57BuildRequestProcessor : BuildRequestMonitor
    {
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;
        private readonly IConfiguration _configuration;
        private readonly IExternalServiceRegistry _externalServiceRegistry;

        public S57BuildRequestProcessor(BuilderContainerService bcs, IConfiguration configuration, IExternalServiceRegistry externalServiceRegistry)
        {
            _containerService = bcs;
            _configuration = configuration;
            _externalServiceRegistry = externalServiceRegistry;
        }

        /// <summary>
        ///     Sets the container environment and starts the container for processing the S57 build request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(S57BuildRequest request, CancellationToken cancellationToken)
        {
            var containerName = $"{ProcessNames.S57Builder}-{request.JobId}";

            var queueConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.QueuesName}"]!;
            var blobConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.BlobsName}"]!;

            var queuePort = ExtractPort(queueConnectionString, "QueueEndpoint");
            var blobPort = ExtractPort(blobConnectionString, "BlobEndpoint");

            var s57FileShareEndpoint = _externalServiceRegistry.GetServiceEndpoint(ProcessNames.FileShareService, "legacy", EndpointHostSubstitution.Docker);
            var s57FileShareUri = s57FileShareEndpoint.Uri;

            var s57FileShareHealthUri = new Uri(s57FileShareUri!, "health");

            // Set the environment variables for the container - in production, these are set from the Azure environment (via the pipeline)
            var containerId = await _containerService.CreateContainerAsync(ProcessNames.S57Builder, containerName, _command, () => new BuilderEnvironment
            {
                AddsEnvironment = AddsEnvironment.Local.Value,
                RequestQueueName = StorageConfiguration.S57BuildRequestQueueName,
                ResponseQueueName = StorageConfiguration.S57BuildResponseQueueName,
                QueueEndpoint = $"http://host.docker.internal:{queuePort}/devstoreaccount1",
                BlobEndpoint = $"http://host.docker.internal:{blobPort}/devstoreaccount1",
                FileShareEndpoint = s57FileShareUri!.ToString(),
                FileShareHealthEndpoint = s57FileShareHealthUri!.ToString(),
                BlobContainerName = StorageConfiguration.S57BuildContainer,
                MaxRetryAttempts = int.Parse(_configuration["buildRequestMonitor:S57:MaxRetries"]!),
                RetryDelayMilliseconds = int.Parse(_configuration["buildRequestMonitor:S57:RetryDelayMilliseconds"]!),
                ConcurrentDownloadLimitCount = 0
            });

            await _containerService.StartContainerAsync(containerId);
        }

    }
}

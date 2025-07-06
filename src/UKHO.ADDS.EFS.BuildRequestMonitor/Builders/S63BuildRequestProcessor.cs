using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S63BuildRequestProcessor : BuildRequestMonitor
    {
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;
        private readonly IConfiguration _configuration;

        public S63BuildRequestProcessor(BuilderContainerService bcs, IConfiguration configuration)
        {
            _containerService = bcs;
            _configuration = configuration;
        }

        /// <summary>
        ///     Sets the container environment and starts the container for processing the S63 build request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(BuildRequest request, CancellationToken cancellationToken)
        {
            var containerName = $"{ProcessNames.S63Builder}-{request.JobId}";

            var queueConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.QueuesName}"]!;
            var blobConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.BlobsName}"]!;
            
            var queuePort = ExtractPort(queueConnectionString, "QueueEndpoint");
            var blobPort = ExtractPort(blobConnectionString, "BlobEndpoint");

            // Set the environment variables for the container - in production, these are set from the Azure environment (via the pipeline)
            var containerId = await _containerService.CreateContainerAsync(ProcessNames.S63Builder, containerName, _command, request, env =>
            {
                env.AddsEnvironment = AddsEnvironment.Local.Value;
                env.RequestQueueName = StorageConfiguration.S63BuildRequestQueueName;
                env.ResponseQueueName = StorageConfiguration.S63BuildResponseQueueName;
                env.QueueConnectionString = $"http://host.docker.internal:{queuePort}/devstoreaccount1"; 
                env.BlobConnectionString = $"http://host.docker.internal:{blobPort}/devstoreaccount1";
                env.FileShareEndpoint = _configuration["Endpoints:S63BuilderFileShare"]!;
                env.BlobContainerName = StorageConfiguration.S63JobContainer;
                env.MaxRetryAttempts = int.Parse(_configuration["S63MaxRetries"]!); 
                env.RetryDelayMilliseconds = int.Parse(_configuration["S63RetryDelayMilliseconds"]!); 
            });

            await _containerService.StartContainerAsync(containerId);
        }

    }
}

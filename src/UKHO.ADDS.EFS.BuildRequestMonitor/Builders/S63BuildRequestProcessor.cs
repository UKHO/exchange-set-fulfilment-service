using UKHO.ADDS.Aspire.Configuration;
using UKHO.ADDS.Aspire.Configuration.Remote;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S63BuildRequestProcessor : BuildRequestMonitor
    {
        // TODO Can remove this now?
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;
        private readonly IConfiguration _configuration;
        private readonly IExternalServiceRegistry _externalServiceRegistry;

        public S63BuildRequestProcessor(BuilderContainerService bcs, IConfiguration configuration, IExternalServiceRegistry externalServiceRegistry)
        {
            _containerService = bcs;
            _configuration = configuration;
            _externalServiceRegistry = externalServiceRegistry;
        }

        /// <summary>
        ///     Sets the container environment and starts the container for processing the S63 build request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(S63BuildRequest request, CancellationToken cancellationToken)
        {
            var containerName = $"{ProcessNames.S63Builder}-{request.JobId}";

            var queueConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.QueuesName}"]!;
            var blobConnectionString = _configuration[$"ConnectionStrings:{StorageConfiguration.BlobsName}"]!;
            
            var queuePort = ExtractPort(queueConnectionString, "QueueEndpoint");
            var blobPort = ExtractPort(blobConnectionString, "BlobEndpoint");

            var s63FileShareEndpoint = await _externalServiceRegistry.GetServiceEndpointAsync(ProcessNames.FileShareService, "legacy", EndpointHostSubstitution.Docker);
            var s63FileShareUri = s63FileShareEndpoint.Uri;

            var s63FileShareHealthUri = new Uri(s63FileShareUri!, "health");

            // Set the environment variables for the container - in production, these are set from the Azure environment (via the pipeline)
            var containerId = await _containerService.CreateContainerAsync(ProcessNames.S63Builder, containerName, _command, request, env =>
            {
                env.AddsEnvironment = AddsEnvironment.Local.Value;
                env.RequestQueueName = StorageConfiguration.S63BuildRequestQueueName;
                env.ResponseQueueName = StorageConfiguration.S63BuildResponseQueueName;
                env.QueueConnectionString = $"http://host.docker.internal:{queuePort}/devstoreaccount1"; 
                env.BlobConnectionString = $"http://host.docker.internal:{blobPort}/devstoreaccount1";
                env.FileShareEndpoint = s63FileShareUri!.ToString();
                env.FileShareHealthEndpoint = s63FileShareHealthUri!.ToString();
                env.BlobContainerName = StorageConfiguration.S63BuildContainer;
                env.MaxRetryAttempts = int.Parse(_configuration["orchestrator:builders:s63:MaxRetries"]!); 
                env.RetryDelayMilliseconds = int.Parse(_configuration["orchestrator:builders:s63:RetryDelayMilliseconds"]!); 
            });

            await _containerService.StartContainerAsync(containerId);
        }

    }
}

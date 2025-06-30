using UKHO.ADDS.Configuration.Schema;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Builders
{
    internal class S100BuildRequestProcessor
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;

        public S100BuildRequestProcessor(BuilderContainerService bcs) => _containerService = bcs;

        /// <summary>
        ///     Sets the container environment and starts the container for processing the S100 build request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessRequestAsync(BuildRequest request, CancellationToken cancellationToken)
        {
            var containerName = $"{ContainerName}{request.JobId}";

            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, request.JobId, request.BatchId, env =>
            {
                env.AddsEnvironment = AddsEnvironment.Local.Value;
                env.RequestQueueName = StorageConfiguration.S100BuildRequestQueueName;
                env.ResponseQueueName = StorageConfiguration.S100BuildResponseQueueName;
                env.QueueConnectionString = "not-used-local"; // Running locally, the container uses the URL-based method for connection to Azurite, so the connection strings are not used
                env.BlobConnectionString = "not-used-local";
                env.BlobContainerName = StorageConfiguration.S100JobContainer;
            });

            await _containerService.StartContainerAsync(containerId);
        }
    }
}

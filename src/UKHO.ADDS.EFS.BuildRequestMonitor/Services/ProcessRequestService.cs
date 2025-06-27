using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    internal class ProcessRequestService
    {
        private const string ImageName = "efs-builder-s100";
        private const string ContainerName = "efs-builder-s100-";
        private readonly string[] _command = ["sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..."];

        private readonly BuilderContainerService _containerService;

        public ProcessRequestService(IConfiguration config, BuilderContainerService bcs) => _containerService = bcs ?? throw new ArgumentNullException(nameof(bcs), "BuilderContainerService cannot be null");

        // This service will handle the processing of requests
        // It will monitor the request queue and process each request accordingly
        public async Task ProcessRequestAsync(string request, CancellationToken cancellationToken)
        {
            var data = JsonCodec.Decode<BuilderRequestQueueMessage>(request);
            var containerName = $"{ContainerName}{data.JobId}";

            var containerId = await _containerService.CreateContainerAsync(ImageName, containerName, _command, data.JobId, data.BatchId);

            await _containerService.StartContainerAsync(containerId);
        }
    }
}

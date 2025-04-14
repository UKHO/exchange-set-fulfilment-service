using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class BuilderContainerService
    {
        private readonly string _builderServiceContainerEndpoint;
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly string _fileShareEndpoint;

        public BuilderContainerService(string fileShareEndpoint, string builderServiceContainerEndpoint, ILoggerFactory loggerFactory)
        {
            _fileShareEndpoint = fileShareEndpoint;
            _builderServiceContainerEndpoint = builderServiceContainerEndpoint;
            _logger = loggerFactory.CreateLogger<BuilderContainerService>();

            DockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
        }

        internal DockerClient DockerClient { get; }

        internal BuilderLogStreamer CreateBuilderLogStreamer() => new(this);

        public async Task EnsureImageExistsAsync(string imageName, string tag = "latest")
        {
            var reference = $"{imageName}:{tag}";

            var images = await DockerClient.Images.ListImagesAsync(new ImagesListParameters { Filters = new Dictionary<string, IDictionary<string, bool>> { ["reference"] = new Dictionary<string, bool> { [reference] = true } } });

            if (images.Count > 0)
            {
                _logger.LogInformation("Image '{reference}' already exists", reference);
                return;
            }

            _logger.LogInformation("Image '{reference}' not found. Pulling...", reference);

            await DockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = imageName, Tag = tag },
                null,
                new Progress<JSONMessage>(msg =>
                {
                    if (!string.IsNullOrWhiteSpace(msg.Status))
                    {
                        Console.WriteLine($"{msg.Status} {msg.ProgressMessage ?? ""}");
                    }
                }));

            _logger.LogInformation("Image '{reference}' pulled successfully", reference);
        }

        public async Task<string> CreateContainerAsync(string image, string name, string[] command, string id)
        {
            var response = await DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Env = new List<string> { $"{BuilderEnvironmentVariables.JobId}={id}", $"{BuilderEnvironmentVariables.FileShareEndpoint}={_fileShareEndpoint}", $"{BuilderEnvironmentVariables.BuildServiceEndpoint}={_builderServiceContainerEndpoint}" },
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD-SHELL", "echo healthy" },
                    Interval = TimeSpan.FromSeconds(3),
                    Timeout = TimeSpan.FromSeconds(2),
                    Retries = 3,
                    StartPeriod = (long)TimeSpan.FromSeconds(2).TotalMilliseconds * 1000000
                },
                HostConfig = new HostConfig { RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.OnFailure, MaximumRetryCount = 3 } }
            });

            _logger.LogInformation("Created container with ID: {response.ID}", response.ID);
            return response.ID;
        }

        public async Task StartContainerAsync(string containerId)
        {
            var started = await DockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                throw new Exception("Failed to start container");
            }
        }

        public async Task<long> WaitForContainerExitAsync(string containerId, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var response = await DockerClient.Containers.WaitContainerAsync(containerId, cts.Token);

            if (response.Error != null)
            {
                _logger.LogError("Container reported error: {response.Error.Message}", response.Error.Message);
            }

            return response.StatusCode;
        }

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await DockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        public async Task RemoveContainerAsync(string containerId, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Removing container {containerId}", containerId);
            await DockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, stoppingToken);
        }

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}

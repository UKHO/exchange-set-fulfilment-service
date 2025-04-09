using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    public class BuilderContainerService
    {
        private readonly string _builderServiceContainerEndpoint;

        private readonly string _fileShareEndpoint;
        private readonly string _salesCatalogueEndpoint;

        public BuilderContainerService(string fileShareEndpoint, string salesCatalogueEndpoint, string builderServiceContainerEndpoint)
        {
            _fileShareEndpoint = fileShareEndpoint;
            _salesCatalogueEndpoint = salesCatalogueEndpoint;
            _builderServiceContainerEndpoint = builderServiceContainerEndpoint;

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
                Log.Information($"Image '{reference}' already exists");
                return;
            }

            Log.Information($"Image '{reference}' not found. Pulling...");

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

            Log.Information($"Image '{reference}' pulled successfully");
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
                Env = new List<string> { $"{BuilderEnvironmentVariables.JobId}={id}", $"{BuilderEnvironmentVariables.FileShareEndpoint}={_fileShareEndpoint}", $"{BuilderEnvironmentVariables.SalesCatalogueEndpoint}={_salesCatalogueEndpoint}", $"{BuilderEnvironmentVariables.BuildServiceEndpoint}={_builderServiceContainerEndpoint}" },
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

            Log.Information($"Created container with ID: {response.ID}");
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
                Log.Error($"Container reported error: {response.Error.Message}");
            }

            return response.StatusCode;
        }

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await DockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        public async Task RemoveContainerAsync(string containerId, CancellationToken stoppingToken)
        {
            Log.Information($"Removing container {containerId}");
            await DockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, stoppingToken);
        }

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}

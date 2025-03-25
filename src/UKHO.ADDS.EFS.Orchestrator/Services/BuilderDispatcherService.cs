using System.Runtime.InteropServices;
using System.Threading.Channels;
using Docker.DotNet.Models;
using Docker.DotNet;
using Serilog;
using UKHO.ADDS.EFS.Common.Messages;
using System.Text;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        const string ImageName = "efs-builder-s100";
        const string ContainerName = "efs-builder-s100-";

        readonly string[] _command = new[] { "sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..." };

        readonly TimeSpan _containerTimeout = TimeSpan.FromSeconds(20);

        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly SemaphoreSlim _concurrencyLimiter;

        public BuilderDispatcherService(Channel<ExchangeSetRequestMessage> channel, IConfiguration configuration)
        {
            _channel = channel;

            var maxConcurrentBuilders = configuration.GetValue<int>("Builders:MaximumConcurrentBuilders");
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentBuilders, maxConcurrentBuilders);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await _concurrencyLimiter.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessRequest(request, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to process message");
                    }

                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, stoppingToken);
            }
        }

        private async Task ProcessRequest(ExchangeSetRequestMessage request, CancellationToken stoppingToken)
        {
            var sessionId = Guid.NewGuid().ToString("N");
            var containerName = $"{ContainerName}{sessionId}";

            using var docker = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
            await EnsureImageExistsAsync(docker, ImageName);

            var containerId = await CreateContainerAsync(docker, ImageName, containerName, _command);
            await StartContainerAsync(docker, containerId);

            var logTask = DockerLogStreamer.StreamLogsAsync(
                docker,
                containerId,
                logStdout: line =>
                {
                    Log.Information($"{containerName}] {line}");
                },
                logStderr: line =>
                {
                    Log.Error($"{containerName}] {line}");
                },
                cancellationToken: stoppingToken
            );

            try
            {
                var exitCode = await WaitForContainerExitAsync(docker, containerId, _containerTimeout);
                Log.Information($"Container {containerId} exited with code: {exitCode}");
            }
            catch (TimeoutException)
            {
                Log.Error($"Container {containerId} exceeded timeout. Killing...");
                await docker.Containers.StopContainerAsync(containerId, new ContainerStopParameters { }, stoppingToken);
            }

            await logTask;

            Log.Information($"Removing container {containerId}");
            await docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, stoppingToken);
        }

        public static async Task EnsureImageExistsAsync(DockerClient docker, string imageName, string tag = "latest")
        {
            var reference = $"{imageName}:{tag}";

            var images = await docker.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [reference] = true
                    }
                }
            });

            if (images.Count > 0)
            {
                Log.Information($"Image '{reference}' already exists");
                return;
            }

            Log.Information($"Image '{reference}' not found. Pulling...");

            await docker.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = imageName,
                    Tag = tag
                },
                authConfig: null,
                progress: new Progress<JSONMessage>(msg =>
                {
                    if (!string.IsNullOrWhiteSpace(msg.Status))
                    {
                        Console.WriteLine($"{msg.Status} {(msg.ProgressMessage ?? "")}");
                    }
                }));

            Log.Information($"Image '{reference}' pulled successfully");
        }

        static async Task<string> CreateContainerAsync(DockerClient client, string image, string name, string[] command)
        {
            var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD-SHELL", "echo healthy" },
                    Interval = TimeSpan.FromSeconds(3),
                    Timeout = TimeSpan.FromSeconds(2),
                    Retries = 3,
                    StartPeriod = (long)TimeSpan.FromSeconds(2).TotalMilliseconds * 1000000,
                },
                HostConfig = new HostConfig
                {
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.OnFailure,
                        MaximumRetryCount = 3
                    }
                }
            });

            Log.Information($"Created container with ID: {response.ID}");
            return response.ID;
        }

        static async Task StartContainerAsync(DockerClient client, string containerId)
        {
            var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                throw new Exception("Failed to start container");
            }
        }

        static async Task<long> WaitForContainerExitAsync(DockerClient client, string containerId, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var response = await client.Containers.WaitContainerAsync(containerId, cts.Token);

            if (response.Error != null)
            {
                Log.Error($"Container reported error: {response.Error.Message}");
            }

            return response.StatusCode;
        }

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");
    }
}

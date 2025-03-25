using System.Runtime.InteropServices;
using System.Threading.Channels;
using Docker.DotNet.Models;
using Docker.DotNet;
using Serilog;
using UKHO.ADDS.EFS.Common.Messages;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using Azure.Storage.Queues;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Orchestrator.Services
{
    internal class BuilderDispatcherService : BackgroundService
    {
        const string ImageName = "efs-builder-s100";
        const string ContainerName = "efs-builder-s100-";

        readonly string[] _command = new[] { "sh", "-c", "echo Starting; sleep 5; echo Healthy now; sleep 5; echo Exiting..." };

        readonly TimeSpan _containerTimeout = TimeSpan.FromSeconds(20);

        private readonly Channel<ExchangeSetRequestMessage> _channel;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly BuilderStartup _builderStartup;
        private readonly SemaphoreSlim _concurrencyLimiter;

        public BuilderDispatcherService(Channel<ExchangeSetRequestMessage> channel, QueueServiceClient queueServiceClient, IConfiguration configuration)
        {
            _channel = channel;
            _queueServiceClient = queueServiceClient;

            var builderStartupValue = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.BuilderStartup);
            if (builderStartupValue == null)
            {
                throw new InvalidOperationException($"Environment variable {OrchestratorEnvironmentVariables.BuilderStartup} is not set");
            }

            _builderStartup = Enum.Parse<BuilderStartup>(builderStartupValue);

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
                        var id = Guid.NewGuid().ToString("N");

                        var queueName = _builderStartup switch
                        {
                            BuilderStartup.Orchestrator => $"builder-{id}",
                            BuilderStartup.Manual => "builder-manual",
                            _ => string.Empty
                        };

                        switch (_builderStartup)
                        {
                            case BuilderStartup.Manual:
                                await WriteMessageToQueueAsync(queueName, request);
                                break;

                            case BuilderStartup.Orchestrator:
                                await WriteMessageToQueueAsync(queueName, request);
                                await ExecuteBuilder(request, stoppingToken, queueName, id);
                                break;
                        }
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

        private async Task WriteMessageToQueueAsync(string queueName, ExchangeSetRequestMessage request)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();

            var message = JsonCodec.Encode(request);
            await queueClient.SendMessageAsync(message);
        }

        private async Task DeleteQueueAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.DeleteIfExistsAsync();
        }

        private async Task ExecuteBuilder(ExchangeSetRequestMessage request, CancellationToken stoppingToken, string queueName, string id)
        {
            var containerName = $"{ContainerName}{id}";

            using var docker = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
            await EnsureImageExistsAsync(docker, ImageName);

            var containerId = await CreateContainerAsync(docker, ImageName, containerName, _command, queueName);
            await StartContainerAsync(docker, containerId);

            var logTask = DockerLogStreamer.StreamLogsAsync(
                docker,
                containerId,
                logStdout: line =>
                {
                    Log.Information($"[{containerName}] {line}");
                },
                logStderr: line =>
                {
                    Log.Error($"[{containerName}] {line}");
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
            await DeleteQueueAsync(queueName);

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

        static async Task<string> CreateContainerAsync(DockerClient client, string image, string name, string[] command, string queueName)
        {
            var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Env = new List<string>
                {
                    $"{BuilderEnvironmentVariables.QueueName}={queueName}" 
                },
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

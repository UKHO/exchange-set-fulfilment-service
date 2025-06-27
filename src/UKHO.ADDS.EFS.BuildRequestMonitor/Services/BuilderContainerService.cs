using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using CliWrap;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Services
{
    public class BuilderContainerService
    {
        private readonly ILogger<BuilderContainerService> _logger;
        private readonly string _workspaceAuthenticationKey;
        private readonly string _fileShareBuilderEndpoint;

        public BuilderContainerService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<BuilderContainerService>();
            var mockEndpoint = config[$"services:{ContainerConfiguration.MockContainerName}:http:0"] ?? string.Empty;
            var fssBuilderEndpoint = new UriBuilder(mockEndpoint) { Host = "host.docker.internal", Path = "fss/" };
            _fileShareBuilderEndpoint = fssBuilderEndpoint.Uri.ToString();

            _workspaceAuthenticationKey = config["WorkspaceKey"] ?? string.Empty;

            var test = new BuilderRequestQueueMessage { BatchId = "test", JobId = "test", StorageAddress = "test", CorrelationId = "12345671898" };
            var data =JsonCodec.Encode(test);

            DockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();
        }

        internal DockerClient DockerClient { get; }


        public async Task EnsureImageExistsAsync(string imageName, string tag = "latest")
        {
            var reference = $"{imageName}:{tag}";

            var images = await DockerClient.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { ["reference"] = new Dictionary<string, bool> { [reference] = true } }
            });

            return;

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            const string arguments = $"build -t {ContainerConfiguration.S100BuilderContainerName} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

            // 'docker' writes everything to stderr...

            var result = await Cli.Wrap("docker")
                .WithArguments(arguments)
                .WithWorkingDirectory(srcDirectory)
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(Log.Information))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Information))
                .ExecuteAsync();

            if (result.IsSuccess)
            {
                Log.Information($"{ContainerConfiguration.S100BuilderContainerName} built ok");
            }
            else
            {
                throw new Exception("Failed to create S-100 builder container image");
            }

            //await DockerClient.Images.CreateImageAsync(
            //    new ImagesCreateParameters { FromImage = imageName, Tag = tag },
            //    null,
            //    new Progress<JSONMessage>(msg =>
            //    {
            //        if (!string.IsNullOrWhiteSpace(msg.Status))
            //        {
            //            Console.WriteLine($"{msg.Status} {msg.ProgressMessage ?? ""}");
            //        }
            //    }));
        }

        public async Task<string> CreateContainerAsync(string image, string name, string[] command, string id, string batchId)
        {
            var response = await DockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = name,
                Cmd = command,
                AttachStdout = true,
                AttachStderr = true,
                Tty = false,
                Env = new List<string>
                {
                    $"{BuilderEnvironmentVariables.JobId}={id}",
                    $"{BuilderEnvironmentVariables.FileShareEndpoint}={_fileShareBuilderEndpoint}",
                    //$"{BuilderEnvironmentVariables.BuildServiceEndpoint}={_builderServiceContainerEndpoint}",
                    //$"{BuilderEnvironmentVariables.OtlpEndpoint}={_otlpContainerEndpoint}",
                    $"{BuilderEnvironmentVariables.WorkspaceKey}={_workspaceAuthenticationKey}",
                    $"{BuilderEnvironmentVariables.BatchId}={batchId}",
                },
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

            return response.ID;
        }

        public async Task StartContainerAsync(string containerId)
        {
            var started = await DockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            if (!started)
            {
                //_logger.LogContainerStartFailed(containerId); // Rhz:
                throw new Exception("Failed to start container");
            }
        }

        //public async Task<long> WaitForContainerExitAsync(string containerId, TimeSpan timeout)
        //{
        //    using var cts = new CancellationTokenSource(timeout);
        //    var response = await DockerClient.Containers.WaitContainerAsync(containerId, cts.Token);

        //    if (response.Error != null)
        //    {
        //        _logger.LogContainerWaitFailed(containerId, response.Error.Message);
        //    }

        //    return response.StatusCode;
        //}

        public async Task StopContainerAsync(string containerId, CancellationToken stoppingToken) => await DockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), stoppingToken);

        //public async Task RemoveContainerAsync(string containerId, CancellationToken stoppingToken)
        //{
        //    _logger.LogContainerRemoved(containerId);
        //    await DockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, stoppingToken);
        //}

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");

    }
}

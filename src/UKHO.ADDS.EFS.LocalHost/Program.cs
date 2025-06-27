using System.Runtime.InteropServices;
using CliWrap;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;
using Serilog;
using UKHO.ADDS.Configuration.Aspire;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.LocalHost.Extensions;
 
namespace UKHO.ADDS.EFS.LocalHost
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("ADDS EFS Local Host Aspire Orchestrator");

            var builder = DistributedApplication.CreateBuilder(args);
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);
            var storageBlob = storage.AddBlobs(StorageConfiguration.BlobsName);

            // ADDS Mock
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>(ContainerConfiguration.MockContainerName)
                .WithDashboard("Dashboard")
                .WithExternalHttpEndpoints();

            // Build Request Monitor
            IResourceBuilder<ProjectResource>? requestMonitor = null;

            if (builder.Environment.IsDevelopment())
            {
                requestMonitor = builder.AddProject<UKHO_ADDS_EFS_BuildRequestMonitor>("request-monitor")  //ContainerConfiguration.BuildRequestMonitorName
                    .WithReference(storageQueue)
                    .WaitFor(storageQueue)
                    .WithReference(mockService)
                    .WaitFor(mockService)
                    .WithEnvironment(c =>
                    {
                        c.EnvironmentVariables.Add("WorkspaceKey", "D89D11D265B19CA5C2BE97A7FCB1EF21");
                    })
                    .WithReference(storageBlob)
                    .WaitFor(storageBlob);

            }

            // Orchestrator
            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(storageBlob)
                .WaitFor(storageBlob)
                .WithReference(mockService)
                .WaitFor(mockService)
                .WithExternalHttpEndpoints()
                .WithScalar("API Browser");

            if (builder.Environment.IsDevelopment())
            {
                orchestratorService.WaitFor(requestMonitor!);
            }

            // Configuration
            var configurationService = builder.AddConfiguration(@"..\..\config\configuration.json", tb =>
            {
                tb.AddEndpoint("mockfss", mockService, false, null, "fss");
                tb.AddEndpoint("mockscs", mockService, false, null, "scs");

                tb.AddEndpoint("buildermockfss", mockService, false, "host.docker.internal", "fss");

                tb.AddEndpoint("builderorchestrator", orchestratorService, false, "host.docker.internal", null);
            })
            .WithExternalHttpEndpoints();

            orchestratorService.WithConfiguration(configurationService);

            await CreateBuilderContainerImages();

            var application = builder.Build();

            await application.RunAsync();

            return 0;
        }

        private const string ImageName = "efs-builder-s100";
        private const string Tag = "latest";

        private static async Task CreateBuilderContainerImages()
        {
            // Check to see if we need to build any images

            // S-100
            var reference = $"{ImageName}:{Tag}";

            var dockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();

            var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { ["reference"] = new Dictionary<string, bool> { [reference] = true } }
            });

            foreach (var response in images)
            {
                if (response.RepoTags.Any(x => x.Contains(reference)))
                {
                    return;
                }
            }

            Log.Information("Creating S-100 builder container image...");

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
        }
        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}

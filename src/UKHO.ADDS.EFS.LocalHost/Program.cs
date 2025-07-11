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
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>(ProcessNames.MockService)
                .WithDashboard("Dashboard")
                .WithExternalHttpEndpoints();

            // Build Request Monitor
            IResourceBuilder<ProjectResource>? requestMonitor = null;

            if (builder.Environment.IsDevelopment())
            {
                requestMonitor = builder.AddProject<UKHO_ADDS_EFS_BuildRequestMonitor>(ProcessNames.RequestMonitorService)
                    .WithReference(storageQueue)
                    .WaitFor(storageQueue)
                    .WithReference(mockService)
                    .WaitFor(mockService)
                    .WithReference(storageBlob)
                    .WaitFor(storageBlob);

            }

            // Orchestrator
            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ProcessNames.OrchestratorService)
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
                tb.AddEndpoint("s100mockfss", mockService, false, null, "fss");
                tb.AddEndpoint("s100mockscs", mockService, false, null, "scs");
                tb.AddEndpoint("s100buildermockfss", mockService, false, "host.docker.internal", "fss");

                tb.AddEndpoint("s63mockfss", mockService, false, null, "fss6357");
                tb.AddEndpoint("s63mockscs", mockService, false, null, "scs6357");
                tb.AddEndpoint("s63buildermockfss", mockService, false, "host.docker.internal", "fss6357");

                tb.AddEndpoint("s57mockfss", mockService, false, null, "fss6357");
                tb.AddEndpoint("s57mockscs", mockService, false, null, "scs6357");
                tb.AddEndpoint("s57buildermockfss", mockService, false, "host.docker.internal", "fss6357");
            })
            .WithExternalHttpEndpoints();

            orchestratorService.WithConfiguration(configurationService);

            if (builder.Environment.IsDevelopment())
            {
                requestMonitor!.WithConfiguration(configurationService);
            }

            await CreateBuilderContainerImages(ProcessNames.S100Builder, "latest", "UKHO.ADDS.EFS.Builder.S100");
            await CreateBuilderContainerImages(ProcessNames.S63Builder, "latest", "UKHO.ADDS.EFS.Builder.S63");
            await CreateBuilderContainerImages(ProcessNames.S57Builder, "latest", "UKHO.ADDS.EFS.Builder.S57");

            var application = builder.Build();

            await application.RunAsync();

            return 0;
        }

        private static async Task CreateBuilderContainerImages(string name, string tag, string projectName)
        {
            // Check to see if we need to build any images
            var reference = $"{name}:{tag}";

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

            Log.Information($"Creating {name} builder container image...");

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            var arguments = $"build -t {name} -f ./{projectName}/Dockerfile .";

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
                Log.Information($"{name} built ok");
            }
            else
            {
                throw new Exception($"Failed to create {name} builder container image");
            }
        }

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}

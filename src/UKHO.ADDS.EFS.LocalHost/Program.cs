using System.Runtime.InteropServices;
using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Network;
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
    /// <summary>
    /// Defines the resources required by Aspire. If there are changes and the infrastructure IaC needs to be regenerated then please see Regenerating infra.md.
    /// </summary>
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

            // Create id for existing subnet
            var subnetSubscription = builder.AddParameter("subnetSubscription");
            var subnetResourceGroup = builder.AddParameter("subnetResourceGroup");
            var subnetVnet = builder.AddParameter("subnetVnet");
            var subnetName = builder.AddParameter("subnetName");
            var subnetId = SubnetResource.CreateResourceIdentifier("${subnetSubscription}", "${subnetResourceGroup}", "${subnetVnet}", "${subnetName}");

            // Container apps environment
            var acaEnv = builder.AddAzureContainerAppEnvironment(ServiceConfiguration.AcaEnvironmentName)
                .WithParameter("subnetSubscription", subnetSubscription)
                .WithParameter("subnetResourceGroup", subnetResourceGroup)
                .WithParameter("subnetVnet", subnetVnet)
                .WithParameter("subnetName", subnetName);
            acaEnv.ConfigureInfrastructure(config =>
            {
                var containerEnvironment = config.GetProvisionableResources().OfType<ContainerAppManagedEnvironment>().Single();
                containerEnvironment.VnetConfiguration = new ContainerAppVnetConfiguration
                {
                    InfrastructureSubnetId = new BicepValue<ResourceIdentifier>(subnetId),
                    IsInternal = true
                };
                containerEnvironment.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
            });

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); });
            storage.ConfigureInfrastructure(config =>
            {
                var storageAccount = config.GetProvisionableResources().OfType<StorageAccount>().Single();
                storageAccount.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                storageAccount.AllowSharedKeyAccess = true;
            });

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
            var configurationService = builder.AddConfiguration(@"../../config/configuration.json", tb =>
            {
                tb.AddEndpoint("mockfss", mockService, false, null, "fss");
                tb.AddEndpoint("mockscs", mockService, false, null, "scs");

                tb.AddEndpoint("buildermockfss", mockService, false, "host.docker.internal", "fss");
            }, ServiceConfiguration.ServiceName)
            .WithExternalHttpEndpoints();

            orchestratorService.WithConfiguration(configurationService);

            if (builder.Environment.IsDevelopment())
            {
                requestMonitor!.WithConfiguration(configurationService);
            }

            if (builder.Environment.IsDevelopment())
            {
                await CreateBuilderContainerImages();
            }

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

            const string arguments = $"build -t {ProcessNames.S100Builder} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

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
                Log.Information($"{ProcessNames.S100Builder} built ok");
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

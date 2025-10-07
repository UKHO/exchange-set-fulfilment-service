using System.Runtime.InteropServices;
using Aspire.Hosting.Azure;
using CliWrap;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.Aspire.Configuration.Hosting;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.LocalHost.Extensions;

namespace UKHO.ADDS.EFS.LocalHost
{
    /// <summary>
    /// Defines the resources required by Aspire. If there are changes and the infrastructure IaC needs to be regenerated then please see 'Regenerating infra.md'.
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

            await BuildEfs(builder);

            var application = builder.Build();

            await application.RunAsync();

            return 0;
        }

        private static async Task BuildEfs(IDistributedApplicationBuilder builder)
        {
            // Get parameters
            var efsServiceIdentityName = builder.AddParameter("efsServiceIdentityName");
            var efsRetainResourceGroup = builder.AddParameter("efsRetainResourceGroup");
            var efsContainerAppsEnvironmentName = builder.AddParameter("efsContainerAppsEnvironmentName");
            var efsContainerRegistryName = builder.AddParameter("efsContainerRegistryName");
            var efsApplicationInsightsName = builder.AddParameter("efsApplicationInsightsName");
            var efsEventHubsNamespaceName = builder.AddParameter("efsEventHubsNamespaceName");
            var efsAppConfigurationName = builder.AddParameter("efsAppConfigurationName");
            var efsStorageAccountName = builder.AddParameter("efsStorageAccountName");
            var addsEnvironment = builder.AddParameter("addsEnvironment");
            var orchestratorCpu = builder.AddParameter("orchestratorCpu");
            var orchestratorMemory = builder.AddParameter("orchestratorMemory");
            var elasticAPMApiKey = builder.AddParameter("elasticAPMApiKey");
            var elasticAPMServerUrl = builder.AddParameter("elasticAPMServerURL");


            // Existing user managed identity
            var efsServiceIdentity = builder.AddAzureUserAssignedIdentity(ServiceConfiguration.EfsServiceIdentity).PublishAsExisting(efsServiceIdentityName, efsRetainResourceGroup);

            // App insights
            var appInsights = builder.ExecutionContext.IsPublishMode
                ? builder.AddAzureApplicationInsights(ServiceConfiguration.AppInsightsName).PublishAsExisting(efsApplicationInsightsName, null)
                : null;

            // Event hubs
            var eventHubs = builder.ExecutionContext.IsPublishMode
                ? builder.AddAzureEventHubs(ServiceConfiguration.EventHubsNamespaceName).PublishAsExisting(efsEventHubsNamespaceName, efsRetainResourceGroup)
                : null;

            // Container registry
            var acr = builder.AddAzureContainerRegistry(ServiceConfiguration.ContainerRegistryName).PublishAsExisting(efsContainerRegistryName, null);

            // Container apps environment
            var acaEnv = builder.AddAzureContainerAppEnvironment(ServiceConfiguration.AcaEnvironmentName).PublishAsExisting(efsContainerAppsEnvironmentName, efsRetainResourceGroup);

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); }).PublishAsExisting(efsStorageAccountName, null);
            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);
            var storageBlob = storage.AddBlobs(StorageConfiguration.BlobsName);

            // Redis cache
            var redisCache = builder.AddRedis(ProcessNames.RedisCache)
                .WithRedisInsight()
                .PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                });

            // ADDS Mock
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>(ProcessNames.MockService)
                .WithDashboard("Dashboard")
                .WithExternalHttpEndpoints()
                .PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                });

            // Build Request Monitor
            IResourceBuilder<ProjectResource>? requestMonitor = null;

            if (builder.ExecutionContext.IsRunMode)
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
                .WithReference(redisCache)
                .WaitFor(redisCache)
                .WithAzureUserAssignedIdentity(efsServiceIdentity)
                .WithExternalHttpEndpoints()
                .WithScalar("API Browser")
                .WithEnvironment("ElasticAPM__ApiKey", elasticAPMApiKey)
                .WithEnvironment("ElasticAPM__ServerURL", elasticAPMServerUrl)
                .PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                    var container = app.Template.Containers.Single().Value!;
                    container.Resources.Cpu = orchestratorCpu.AsProvisioningParameter(infra, "orchestratorCpu");
                    container.Resources.Memory = orchestratorMemory.AsProvisioningParameter(infra, "orchestratorMemory");
                });

            if (builder.ExecutionContext.IsPublishMode)
            {
                orchestratorService.WithReference(appInsights!);
                orchestratorService.WaitFor(appInsights!);
                orchestratorService.WithReference(eventHubs!);
                orchestratorService.WaitFor(eventHubs!);
            }

            if (builder.ExecutionContext.IsRunMode)
            {
                orchestratorService.WaitFor(requestMonitor!);
            }

            // Configuration
            if (builder.ExecutionContext.IsRunMode)
            {
                builder.AddConfigurationEmulator(ServiceConfiguration.ServiceName, [orchestratorService, requestMonitor!], [mockService], @"../../configuration/configuration.json", @"../../configuration/external-services.json");
            }
            else
            {
                var appConfig = builder.AddConfiguration(ProcessNames.ConfigurationService, addsEnvironment, [orchestratorService]).PublishAsExisting(efsAppConfigurationName, null);
            }

            if (builder.ExecutionContext.IsRunMode)
            {
                var appRootPath = builder.Environment.ContentRootPath;
                await CreateBuilderContainerImages(ProcessNames.S100Builder, "latest", "UKHO.ADDS.EFS.Builder.S100", appRootPath);
                await CreateBuilderContainerImages(ProcessNames.S63Builder, "latest", "UKHO.ADDS.EFS.Builder.S63", appRootPath);
                await CreateBuilderContainerImages(ProcessNames.S57Builder, "latest", "UKHO.ADDS.EFS.Builder.S57", appRootPath);
            }
        }

        private static async Task CreateBuilderContainerImages(string name, string tag, string projectName, string appRootDirectory)
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

            var srcDirectory = Directory.GetParent(appRootDirectory)?.FullName!;

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

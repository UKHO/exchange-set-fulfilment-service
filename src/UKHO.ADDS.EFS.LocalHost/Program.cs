using CliWrap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
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

            var mockEndpointPort = builder.Configuration.GetValue<int>("Endpoints:MockEndpointPort");
            var mockEndpointContainerPort = builder.Configuration.GetValue<int>("Endpoints:MockEndpointContainerPort");

            var containerRuntime = builder.Configuration.GetValue<ContainerRuntime>("Containers:ContainerRuntime");
            var buildOnStartup = builder.Configuration.GetValue<bool>("Containers:BuildOnStartup");

            // Storage configuration

            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);
            var storageBlob = storage.AddBlobs(StorageConfiguration.BlobsName);

            // ADDS Mock
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>("adds-mocks-efs")
                .WithDashboard("Dashboard");

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
                .WithScalar("API Browser");

            orchestratorService.WithEnvironment(c =>
            {
                var addsMockEndpoint = mockService.GetEndpoint("http");
                var fssEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = "host.docker.internal", Path = "fss" };

                var scsEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.Host, Path = "scs" };

                var orchestratorServiceEndpoint = orchestratorService.GetEndpoint("http").Url;

                c.EnvironmentVariables[OrchestratorEnvironmentVariables.FileShareEndpoint] = fssEndpoint.ToString();
                c.EnvironmentVariables[OrchestratorEnvironmentVariables.SalesCatalogueEndpoint] = scsEndpoint.ToString();
                c.EnvironmentVariables[OrchestratorEnvironmentVariables.BuildServiceEndpoint] = orchestratorServiceEndpoint;
            });

            builder.Services.AddHttpClient();

            if (buildOnStartup)
            {
                await CreateS100BuilderContainerImage(containerRuntime);
            }

            await builder.Build().RunAsync();

            return 0;
        }

        private static async Task CreateS100BuilderContainerImage(ContainerRuntime containerRuntime)
        {
            Log.Information("Creating S-100 builder container image...");
            Log.Information($"Using container runtime '{containerRuntime}'");

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            const string arguments = $"build -t {ContainerConfiguration.S100BuilderContainerName} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

            // 'docker' writes everything to stderr...

            var result = await Cli.Wrap(containerRuntime.ToString().ToLowerInvariant())
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
    }
}

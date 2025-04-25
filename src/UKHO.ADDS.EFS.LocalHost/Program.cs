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

            var addsMockContainer = builder.AddDockerfile(ContainerConfiguration.MockContainerName, @"..\..\mock\repo\src\ADDSMock")
                .WithHttpEndpoint(mockEndpointPort, mockEndpointContainerPort, ContainerConfiguration.MockContainerEndpointName);

            var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch:7.17.0")
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithHttpEndpoint(9200, 9200);

            var kibanaContainer = builder.AddContainer("kibana", "docker.elastic.co/kibana/kibana:7.17.0")
                .WithEnvironment("ELASTICSEARCH_HOSTS", "http://elasticsearch:9200")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithHttpEndpoint(5601, 5601)
                .WaitFor(elasticsearch);

            builder.AddContainer("apm-server", "docker.elastic.co/apm/apm-server:7.17.0")
                .WithEnvironment("setup.kibana.host", "http://kibana:5601")
                .WithEnvironment("setup.dashboards.enabled", "true")
                .WithEnvironment("output.elasticsearch.hosts", "[\"http://elasticsearch:9200\"]")
                .WithEnvironment("apm-server.host", "0.0.0.0:8200")
                .WithHttpEndpoint(8200, 8200)
                .WaitFor(elasticsearch);

            // Orchestrator

            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(storageBlob)
                .WaitFor(storageBlob)
                .WaitFor(addsMockContainer)
                .WithScalar("API Browser")
                .WithKibanaDashboard(kibanaContainer.GetEndpoint("http"), "Kibana dashboard");

            orchestratorService.WithEnvironment(c =>
            {
                var addsMockEndpoint = addsMockContainer.GetEndpoint(ContainerConfiguration.MockContainerEndpointName);
                var fssEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.ContainerHost, Path = "fss" };

                var scsEndpoint = new UriBuilder(addsMockEndpoint.Url) {Path = "scs" };

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

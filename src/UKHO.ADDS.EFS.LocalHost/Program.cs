using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration.Namespaces;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using UKHO.ADDS.EFS.LocalHost.Extensions;

namespace UKHO.ADDS.EFS.LocalHost
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("ADDS EFS Local Host Aspire Orchestrator");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            var wasBlobPort = config.GetValue<int>("Endpoints:WASBlobPort");
            var wasQueuePort = config.GetValue<int>("Endpoints:WASQueuePort");
            var wasTablePort = config.GetValue<int>("Endpoints:WASTablePort");

            var mockEndpointPort = config.GetValue<int>("Endpoints:MockEndpointPort");
            var mockEndpointContainerPort = config.GetValue<int>("Endpoints:MockEndpointContainerPort");

            var builderStartup = config.GetValue<BuilderStartup>("Orchestrator:BuilderStartup");

            var builder = DistributedApplication.CreateBuilder(args);

            // Service bus configuration
            var serviceBus = builder.AddAzureServiceBus(ServiceBusConfiguration.ServiceBusName).RunAsEmulator();

            serviceBus.AddServiceBusTopic(ServiceBusConfiguration.TopicName)
                .AddServiceBusSubscription(ServiceBusConfiguration.SubscriptionName);

            // Storage configuration

            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e =>
            {
                e.WithDataVolume();

                e.WithBlobPort(wasBlobPort);
                e.WithQueuePort(wasQueuePort);
                e.WithTablePort(wasTablePort);
            });

            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);

            // ADDS Mock

            var addsMockContainer = builder.AddDockerfile(ContainerConfiguration.MockContainerName, @"..\..\mock\repo\src\ADDSMock")
                .WithHttpEndpoint(mockEndpointPort, mockEndpointContainerPort, ContainerConfiguration.MockContainerEndpointName);

            // Orchestrator

            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(serviceBus)
                .WaitFor(serviceBus)
                .WaitFor(addsMockContainer)
                .WithOrchestratorDashboard("Builder dashboard")
                .WithScalar("API documentation")
                .WithEnvironment(OrchestratorEnvironmentVariables.BuilderStartup, builderStartup.ToString);

            await CreateS100BuilderContainerImage();

            await builder.Build().RunAsync();

            return 0;
        }

        private static async Task CreateS100BuilderContainerImage()
        {
            Log.Information("Creating S-100 builder container image...");

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            const string arguments = $"build -t {ContainerConfiguration.S100BuilderContainerName} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

            var result = await Cli.Wrap("docker")
                .WithArguments(arguments)
                .WithWorkingDirectory(srcDirectory)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            // 'docker' writes everything to stderr...

            if (result.IsSuccess)
            {
                Log.Information(result.StandardError);
            }
            else
            {
                Log.Fatal(result.StandardError);
                throw new Exception("Failed to create S-100 builder container image");
            }
        }
    }
}

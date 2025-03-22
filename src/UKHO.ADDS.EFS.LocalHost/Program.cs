using k8s.Models;
using Microsoft.Extensions.Configuration;
using Projects;
using UKHO.ADDS.EFS.Common.Configuration;
using UKHO.ADDS.EFS.LocalHost.Extensions;

namespace UKHO.ADDS.EFS.LocalHost
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = DistributedApplication.CreateBuilder(args);

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var wasBlobPort = config.GetValue<int>("WASBlobPort");
            var wasQueuePort = config.GetValue<int>("WASQueuePort");
            var wasTablePort = config.GetValue<int>("WASTablePort");

            var mockEndpointPort = config.GetValue<int>("MockEndpointPort");
            var mockEndpointContainerPort = config.GetValue<int>("MockEndpointContainerPort");

            var fulfilmentEndpointPort = config.GetValue<int>("FulfilmentEndpointPort");
            var fulfilmentEndpointContainerPort = config.GetValue<int>("FulfilmentEndpointContainerPort");

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

            var addsMockContainer = builder.AddDockerfile(ContainerConfiguration.MockContainerName, @"..\..\mock\repo\src\ADDSMock")
                .WithHttpEndpoint(mockEndpointPort, mockEndpointContainerPort, ContainerConfiguration.MockContainerEndpointName);

//            var builderContainer = builder.AddDockerfile(ContainerConfiguration.BuilderContainerName, "..", dockerfilePath: "BuilderDockerfile")
//                .WithHttpEndpoint(fulfilmentEndpointPort, fulfilmentEndpointContainerPort, ContainerConfiguration.BuilderContainerEndpointName);

            await CreateBuilderImage(builder);

            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                //.WithReference(storageQueue)
                //.WaitFor(storageQueue)
                //.WithReference(storageTable)
                //.WaitFor(storageTable)
                //.WithReference(serviceBus)
                //.WaitFor(serviceBus)
                //.WithReference(addsMockContainerEndpoint)
                //.WaitFor(addsMockContainer)
                .WithOrchestratorDashboard("Builder dashboard")
                .WithScalar("API documentation");

            await builder.Build().RunAsync();

            return 0;
        }

        private static async Task CreateBuilderImage(IDistributedApplicationBuilder builder)
        {
            var localHostDirectory = Directory.GetCurrentDirectory();


        }
    }
}

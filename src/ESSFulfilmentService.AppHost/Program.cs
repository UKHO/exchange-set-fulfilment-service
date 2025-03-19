using Aspire.Hosting;
using ESSFulfilmentService.Common.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus(ServiceBusConfiguration.ServiceBusName).RunAsEmulator();
serviceBus.AddServiceBusTopic(name: ServiceBusConfiguration.TopicName)
    .AddServiceBusSubscription(name: ServiceBusConfiguration.SubscriptionName);

var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(
    azurite =>
    {
        azurite.WithDataVolume();
    });

var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);

var iic_custom = builder.AddDockerfile(ContainerConfiguration.BuilderContainerName, "../ESSFulfilmentService.Builder")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: ContainerConfiguration.BuilderContainerEndpointName);

var iic_endpoint = iic_custom.GetEndpoint(ContainerConfiguration.BuilderContainerEndpointName);

builder.AddProject<Projects.ESSFulfilmentService_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
    .WithReference(storageQueue)
    .WaitFor(storageQueue)
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

// possibly keep#
// .WithReference(iic_endpoint)
// .WaitFor(iic_custom)


builder.Build().Run();

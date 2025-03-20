using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ESSFulfilmentService.AppHost.Extensions;
using ESSFulfilmentService.Common.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus(ServiceBusConfiguration.ServiceBusName).RunAsEmulator();

serviceBus.AddServiceBusTopic(name: ServiceBusConfiguration.TopicName)
    .AddServiceBusSubscription(name: ServiceBusConfiguration.SubscriptionName);

var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => e.WithDataVolume());

var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
var storageTable = storage.AddTables(StorageConfiguration.TablesName);

var addsMockContainer = builder.AddDockerfile(ContainerConfiguration.MockContainerName, @"..\..\mock\repo\src\ADDSMock")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: ContainerConfiguration.MockContainerEndpointName );

var addsMockContainerEndpoint = addsMockContainer.GetEndpoint(ContainerConfiguration.MockContainerEndpointName);

var builderContainer = builder.AddDockerfile(ContainerConfiguration.BuilderContainerName, "../ESSFulfilmentService.Builder")
    .WithHttpEndpoint(port: 8081, targetPort: 8080, name: ContainerConfiguration.BuilderContainerEndpointName);

var builderContainerEndpoint = builderContainer.GetEndpoint(ContainerConfiguration.BuilderContainerEndpointName);

builder.AddProject<Projects.ESSFulfilmentService_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
    .WithReference(storageQueue)
    .WaitFor(storageQueue)
    .WithReference(serviceBus)
    .WaitFor(serviceBus)
    .WithReference(builderContainerEndpoint)
    .WaitFor(builderContainer)
    .WithReference(addsMockContainerEndpoint)
    .WaitFor(addsMockContainer);

var apiService = builder.AddProject<Projects.ESSFulfilmentService_API>(ContainerConfiguration.ApiContainerName)
    .WithReference(storageQueue)
    .WaitFor(storageQueue)
    .WithReference(storageTable)
    .WaitFor(storageTable)
    .WithScalar("ESS Fulfilment API documentation");

builder.Build().Run();

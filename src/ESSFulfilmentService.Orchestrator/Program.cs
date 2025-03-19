using ESSFulfilmentService.Common.Configuration;
using ESSFulfilmentService.Orchestrator;

var builder = Host.CreateApplicationBuilder(args);
builder.AddAzureQueueClient(StorageConfiguration.QueuesName);
builder.AddAzureServiceBusClient(ServiceBusConfiguration.ServiceBusName);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

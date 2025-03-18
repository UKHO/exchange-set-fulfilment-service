using ESSFulfilmentService.Orchestrator;

var builder = Host.CreateApplicationBuilder(args);
builder.AddAzureQueueClient("queueConnection");
builder.AddAzureServiceBusClient("service-bus");

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

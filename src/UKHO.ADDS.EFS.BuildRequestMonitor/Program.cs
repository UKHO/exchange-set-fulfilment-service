using UKHO.ADDS.EFS.BuildRequestMonitor;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.BuildRequestMonitor.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.AddAzureQueueClient(StorageConfiguration.QueuesName);

builder.Services.AddTransient<BuilderContainerService>();
builder.Services.AddTransient<ProcessRequestService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

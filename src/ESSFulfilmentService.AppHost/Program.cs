var builder = DistributedApplication.CreateBuilder(args);

var servicebus = builder.AddAzureServiceBus("service-bus").RunAsEmulator();
servicebus.AddServiceBusTopic(name: "iic-topic")
           .AddServiceBusSubscription(name: "iic-subscription");

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
    azurite =>
    {
        azurite.WithDataVolume();
    });
var storagequeue = storage.AddQueues("queueConnection");

// To be replaced with container
//var iic_custom = builder.AddDockerfile("iic",".")
//    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "iic-endpoint");

//var iic_endpoint = iic_custom.GetEndpoint("iic-endpoint");


builder.AddProject<Projects.ESSFulfilmentService_Orchestrator>("orchestrator")
    .WithReference(storagequeue)
    .WaitFor(storagequeue)
    .WithReference(servicebus)
    .WaitFor(servicebus);

// possibly keep#
// .WithReference(iic_endpoint)
// .WaitFor(iic_custom)





builder.Build().Run();

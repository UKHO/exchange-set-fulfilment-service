var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
    azurite =>
    {
        azurite.WithDataVolume();
    });
var queuestorage = storage.AddQueues("queueConnection");


var iic_custom = builder.AddDockerfile("iic",".")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "iic-endpoint");

var iic_endpoint = iic_custom.GetEndpoint("iic-endpoint");

// possibly keep#
// .WithReference(iic_endpoint)
// .WaitFor(iic_custom)


    
    

builder.Build().Run();

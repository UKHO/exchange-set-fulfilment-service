var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
    azurite =>
    {
        azurite.WithDataVolume();
    });
var blobstorage = storage.AddBlobs("blobConnection");
var queuestorage = storage.AddQueues("queueConnection");
var tablestorage = storage.AddTables("tableConnection");

// This will fail because we have not yet created the container image for the IIC service
var iic_custom = builder.AddContainer("iic", "richardahz/iic","latest")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "iic-endpoint");

var iic_endpoint = iic_custom.GetEndpoint("iic-endpoint");

//This won't start because we have not yet created the container image for the IIC service
builder.AddProject<Projects.ESSFulfilmentService_Builder>("essfulfilmentservice-builder")
    .WithReference(blobstorage)
    .WaitFor(blobstorage)
    .WithReference(queuestorage)
    .WaitFor(queuestorage)
    .WithReference(tablestorage)
    .WaitFor(tablestorage)
    .WithReference(iic_endpoint)
    .WaitFor(iic_custom);

builder.Build().Run();

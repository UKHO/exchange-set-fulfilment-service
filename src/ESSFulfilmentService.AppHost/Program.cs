var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator(
    azurite =>
    {
        azurite.WithDataVolume();
    });
var blobstorage = storage.AddBlobs("blobConnection");
var queuestorage = storage.AddQueues("queueConnection");
var tablestorage = storage.AddTables("tableConnection");


var iic_custom = builder.AddDockerfile("iic",".")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "iic-endpoint");

var iic_endpoint = iic_custom.GetEndpoint("iic-endpoint");

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

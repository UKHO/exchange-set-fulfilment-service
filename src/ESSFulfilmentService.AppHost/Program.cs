var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ESSFulfilmentService_Builder>("essfulfilmentservice-builder");

builder.Build().Run();

using CliWrap;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.Configuration.Aspire;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.LocalHost.Extensions;
 
namespace UKHO.ADDS.EFS.LocalHost
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("ADDS EFS Local Host Aspire Orchestrator");

            var builder = DistributedApplication.CreateBuilder(args);
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

            var buildOnStartup = builder.Configuration.GetValue<bool>("Containers:BuildOnStartup");

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); });

            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);
            var storageBlob = storage.AddBlobs(StorageConfiguration.BlobsName);

            // ADDS Mock
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>(ContainerConfiguration.MockContainerName)
                .WithDashboard("Dashboard")
                .WithExternalHttpEndpoints();

            // Orchestrator
            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(storageBlob)
                .WaitFor(storageBlob)
                .WithReference(mockService)
                .WaitFor(mockService)
                .WithExternalHttpEndpoints()
                .WithScalar("API Browser");

            // Configuration
            var configurationService = builder.AddConfiguration(@"..\..\config\configuration.json", tb =>
            {
                tb.AddEndpoint("mockfss", mockService, false, null, "fss");
                tb.AddEndpoint("mockscs", mockService, false, null, "scs");

                tb.AddEndpoint("buildermockfss", mockService, false, "host.docker.internal", "fss");

                tb.AddEndpoint("builderorchestrator", orchestratorService, false, "host.docker.internal", null);
            })
            .WithExternalHttpEndpoints();

            orchestratorService.WithConfiguration(configurationService);

            if (buildOnStartup)
            {
                await CreateS100BuilderContainerImage();
            }

            var application = builder.Build();

            await application.RunAsync();

            return 0;
        }

        private static async Task CreateS100BuilderContainerImage()
        {
            Log.Information("Creating S-100 builder container image...");

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            const string arguments = $"build -t {ContainerConfiguration.S100BuilderContainerName} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

            // 'docker' writes everything to stderr...

            var result = await Cli.Wrap("docker")
                .WithArguments(arguments)
                .WithWorkingDirectory(srcDirectory)
                .WithValidation(CommandResultValidation.None)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(Log.Information))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Information))
                .ExecuteAsync();

            if (result.IsSuccess)
            {
                Log.Information($"{ContainerConfiguration.S100BuilderContainerName} built ok");
            }
            else
            {
                throw new Exception("Failed to create S-100 builder container image");
            }
        }
    }
}

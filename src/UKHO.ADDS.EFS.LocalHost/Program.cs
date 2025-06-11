using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.Aspire.Client;
using AzureKeyVaultEmulator.Aspire.Hosting;
using CliWrap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Projects;
using Serilog;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
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
                .WithDashboard("Dashboard");

            // Key vault
            var keyVault = builder.AddAzureKeyVaultEmulator(ContainerConfiguration.KeyVaultContainerName,
                new KeyVaultEmulatorOptions
                {
                    Persist = false
                });

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
                .WithReference(keyVault)
                .WaitFor(keyVault)
                .WithScalar("API Browser");

            orchestratorService.WithEnvironment(async c =>
            {
                var addsMockEndpoint = mockService.GetEndpoint("http");
                var fssEndpointBuilderHost = new UriBuilder(addsMockEndpoint.Url) { Host = "host.docker.internal", Path = "fss/" };
                var fssEndpointOrchestratorHost = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.Host, Path = "fss/" };
                var scsEndpoint = new UriBuilder(addsMockEndpoint.Url) { Host = addsMockEndpoint.Host, Path = "scs/" };

                var orchestratorEndpoint = orchestratorService.GetEndpoint("http").Url;

                var workspaceKey = "D89D11D265B19CA5C2BE97A7FCB1EF21";

                var secretClient = c.ExecutionContext.ServiceProvider.GetRequiredService<SecretClient>();

                await secretClient.SetSecretAsync(OrchestratorConfigurationKeys.FileShareEndpoint, fssEndpointBuilderHost.Uri.ToString());
                await secretClient.SetSecretAsync(OrchestratorConfigurationKeys.FileShareEndpointOrchestratorHost, fssEndpointOrchestratorHost.Uri.ToString());
                await secretClient.SetSecretAsync(OrchestratorConfigurationKeys.SalesCatalogueEndpoint, scsEndpoint.Uri.ToString());
                await secretClient.SetSecretAsync(OrchestratorConfigurationKeys.OrchestratorServiceEndpoint, orchestratorEndpoint);
                await secretClient.SetSecretAsync(OrchestratorConfigurationKeys.WorkspaceKey, workspaceKey);
            });

            // Fixed endpoint
            var keyVaultUri = "https://localhost:4997";

            builder.Services.AddAzureKeyVaultEmulator(keyVaultUri, secrets: true, keys: true, certificates: false);

            builder.Services.AddHttpClient();

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

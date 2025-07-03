using System.Security.Cryptography;
using System.Security.Principal;
using Azure.Core;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.Storage;
using Azure.ResourceManager.Network;
using CliWrap;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.Configuration.Aspire;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.LocalHost.Extensions;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;

namespace UKHO.ADDS.EFS.LocalHost
{
    /// <summary>
    /// Defines the resources required by Aspire. If there are changes and the infrastructure IaC needs to be regenerated then please see Regenerating infra.md.
    /// </summary>
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

            // Create id for existing subnet
            var subnetSubscription = builder.AddParameter("subnetSubscription");
            var subnetResourceGroup = builder.AddParameter("subnetResourceGroup");
            var subnetVnet = builder.AddParameter("subnetVnet");
            var subnetName = builder.AddParameter("subnetName");
            var subnetId = SubnetResource.CreateResourceIdentifier("${subnetSubscription}", "${subnetResourceGroup}", "${subnetVnet}", "${subnetName}");

            // Container apps environment
            var acaEnv = builder.AddAzureContainerAppEnvironment(ContainerConfiguration.AcaEnvironmentName)
                .WithParameter("subnetSubscription", subnetSubscription)
                .WithParameter("subnetResourceGroup", subnetResourceGroup)
                .WithParameter("subnetVnet", subnetVnet)
                .WithParameter("subnetName", subnetName);
            acaEnv.ConfigureInfrastructure(config =>
            {
                var containerEnvironment = config.GetProvisionableResources().OfType<ContainerAppManagedEnvironment>().Single();
                containerEnvironment.VnetConfiguration = new ContainerAppVnetConfiguration
                {
                    InfrastructureSubnetId = new BicepValue<ResourceIdentifier>(subnetId),
                    IsInternal = true
                };
                containerEnvironment.Tags.Add("hidden-title", SystemConfiguration.SystemName);
            });

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); });
            storage.ConfigureInfrastructure(config =>
            {
                var storageAccount = config.GetProvisionableResources().OfType<StorageAccount>().Single();
                storageAccount.Tags.Add("hidden-title", SystemConfiguration.SystemName);
                storageAccount.AllowSharedKeyAccess = true;
            });

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
            var configurationService = builder.AddConfiguration(@"../../config/configuration.json", tb =>
            {
                tb.AddEndpoint("mockfss", mockService, false, null, "fss");
                tb.AddEndpoint("mockscs", mockService, false, null, "scs");

                tb.AddEndpoint("buildermockfss", mockService, false, "host.docker.internal", "fss");

                tb.AddEndpoint("builderorchestrator", orchestratorService, false, "host.docker.internal", null);
            }, SystemConfiguration.SystemName)
            .WithExternalHttpEndpoints();

            orchestratorService.WithConfiguration(configurationService);

            // Builder job - not currently used locally, but defined to generate the infrastructure.
            var containerAppJob = builder.AddAzureInfrastructure(ContainerConfiguration.S100BuilderContainerName, infra =>
                {
                    var registry = new ContainerAppJob(ContainerConfiguration.S100BuilderContainerName.Replace("-", string.Empty), "2025-01-01")
                    {
                        //EnvironmentId = new BicepValue<string>(acaEnv.GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_ID").Value!)
                    };
                    infra.Add(registry);

                    //_name = DefineProperty<string>("Name", new string[1] { "name" }, isOutput: false, isRequired: true);
                    //_location = DefineProperty<AzureLocation>("Location", new string[1] { "location" }, isOutput: false, isRequired: true);
                    //_configuration = DefineModelProperty<ContainerAppJobConfiguration>("Configuration", new string[2] { "properties", "configuration" });
                    //_environmentId = DefineProperty<string>("EnvironmentId", new string[2] { "properties", "environmentId" });
                    //_identity = DefineModelProperty<ManagedServiceIdentity>("Identity", new string[1] { "identity" });
                    //_tags = DefineDictionaryProperty<string>("Tags", new string[1] { "tags" });
                    //_template = DefineModelProperty<ContainerAppJobTemplate>("Template", new string[2] { "properties", "template" });
                    //_workloadProfileName = DefineProperty<string>("WorkloadProfileName", new string[2] { "properties", "workloadProfileName" });
                    //_eventStreamEndpoint = DefineProperty<string>("EventStreamEndpoint", new string[2] { "properties", "eventStreamEndpoint" }, isOutput: true);
                    //_id = DefineProperty<ResourceIdentifier>("Id", new string[1] { "id" }, isOutput: true);
                    //_outboundIPAddresses = DefineListProperty<string>("OutboundIPAddresses", new string[2] { "properties", "outboundIpAddresses" }, isOutput: true);
                    //_provisioningState = DefineProperty<ContainerAppJobProvisioningState>("ProvisioningState", new string[2] { "properties", "provisioningState" }, isOutput: true);
                    //_systemData = DefineModelProperty<SystemData>("SystemData", new string[1] { "systemData" }, isOutput: true);

                    //var output = new ProvisioningOutput("AZURE_BUILDER_CONTAINER_APP_JOB_NAME", typeof(string))
                    //{
                    //    Value = registry.Name
                    //};
                    //infra.Add(output);
                });//.WithReferenceRelationship(acaEnv);

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

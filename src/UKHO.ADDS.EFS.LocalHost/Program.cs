using System.Runtime.InteropServices;
using System.Text;
using Aspire.Hosting.Azure;
using CliWrap;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.Aspire.Configuration.Hosting;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;
using UKHO.ADDS.EFS.LocalHost.Extensions;

namespace UKHO.ADDS.EFS.LocalHost
{
    /// <summary>
    /// Defines the resources required by Aspire. If there are changes and the infrastructure IaC needs to be regenerated then please see 'Regenerating infra.md'.
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

            await BuildEfs(builder);

            var application = builder.Build();

            await application.RunAsync();

            return 0;
        }

        private static async Task BuildEfs(IDistributedApplicationBuilder builder)
        {
            // Get parameters
            var efsServiceIdentityName = builder.AddPublishOnlyParameter("efsServiceIdentityName");
            var efsRetainResourceGroup = builder.AddPublishOnlyParameter("efsRetainResourceGroup");
            var efsContainerAppsEnvironmentName = builder.AddPublishOnlyParameter("efsContainerAppsEnvironmentName");
            var efsContainerRegistryName = builder.AddPublishOnlyParameter("efsContainerRegistryName");
            var efsApplicationInsightsName = builder.AddPublishOnlyParameter("efsApplicationInsightsName");
            var efsEventHubsNamespaceName = builder.AddPublishOnlyParameter("efsEventHubsNamespaceName");
            var efsAppConfigurationName = builder.AddPublishOnlyParameter("efsAppConfigurationName");
            var efsStorageAccountName = builder.AddPublishOnlyParameter("efsStorageAccountName");
            var addsEnvironment = builder.AddPublishOnlyParameter("addsEnvironment");
            var addsMocksCpu = builder.AddPublishOnlyParameter("addsMocksCpu");
            var addsMocksMemory = builder.AddPublishOnlyParameter("addsMocksMemory");
            var orchestratorCpu = builder.AddPublishOnlyParameter("orchestratorCpu");
            var orchestratorMemory = builder.AddPublishOnlyParameter("orchestratorMemory");
            var elasticApmApiKey = builder.AddPublishOnlyParameter("elasticAPMApiKey", true);
            var elasticApmServerUrl = builder.AddPublishOnlyParameter("elasticAPMServerURL");
            var elasticApmServiceName = builder.AddPublishOnlyParameter("elasticAPMServiceName");
            var elasticApmEnvironment = builder.AddPublishOnlyParameter("elasticAPMEnvironment");
            var efsAppRegClientId = builder.AddPublishOnlyParameter("efsAppRegClientId");
            var efsAppRegTenantId = builder.AddPublishOnlyParameter("efsAppRegTenantId");
            var efsB2cAppClientId = builder.AddPublishOnlyParameter("efsB2cAppClientId");
            var efsB2cAppTenantId = builder.AddPublishOnlyParameter("efsB2cAppTenantId");
            var efsB2cAppDomain = builder.AddPublishOnlyParameter("efsB2cAppDomain");
            var efsB2cAppInstance = builder.AddPublishOnlyParameter("efsB2cAppInstance");
            var efsB2cAppSigninPolicy = builder.AddPublishOnlyParameter("efsB2cAppSigninPolicy");

            // Existing user managed identity
            var efsServiceIdentity = builder.AddAzureUserAssignedIdentity(ServiceConfiguration.EfsServiceIdentity).PublishAsExistingWithNullCheck(efsServiceIdentityName, efsRetainResourceGroup);

            // App insights
            var appInsights = builder.ExecutionContext.IsPublishMode
                ? builder.AddAzureApplicationInsights(ServiceConfiguration.AppInsightsName).PublishAsExisting(efsApplicationInsightsName!, null)
                : null;

            // Event hubs
            var eventHubs = builder.ExecutionContext.IsPublishMode
                ? builder.AddAzureEventHubs(ServiceConfiguration.EventHubsNamespaceName).PublishAsExisting(efsEventHubsNamespaceName!, efsRetainResourceGroup)
                : null;

            // Container registry
            var acr = builder.AddAzureContainerRegistry(ServiceConfiguration.ContainerRegistryName).PublishAsExistingWithNullCheck(efsContainerRegistryName, null);

            // Container apps environment
            var acaEnv = builder.AddAzureContainerAppEnvironment(ServiceConfiguration.AcaEnvironmentName).PublishAsExistingWithNullCheck(efsContainerAppsEnvironmentName, efsRetainResourceGroup);

            // Storage configuration
            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e => { e.WithDataVolume(); }).PublishAsExistingWithNullCheck(efsStorageAccountName, null);
            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);
            var storageBlob = storage.AddBlobs(StorageConfiguration.BlobsName);

            // Redis cache
            var redisCache = builder.AddRedis(ProcessNames.RedisCache)
                .WithRedisInsight()
                .PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                });

            // ADDS Mock
            var mockService = builder.AddProject<UKHO_ADDS_Mocks_EFS>(ProcessNames.MockService)
                .WithDashboard("Dashboard")
                .WithExternalHttpEndpoints();

            if (builder.ExecutionContext.IsPublishMode)
            {
                mockService.PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                    app.Template.Scale.MinReplicas = 1;
                    app.Template.Scale.MaxReplicas = 1;
                    var container = app.Template.Containers.Single().Value!;
                    container.Resources.Cpu = addsMocksCpu!.AsProvisioningParameter(infra, "addsMocksCpu");
                    container.Resources.Memory = addsMocksMemory!.AsProvisioningParameter(infra, "addsMocksMemory");
                });
            }

            // Build Request Monitor
            IResourceBuilder<ProjectResource>? requestMonitor = null;

            if (builder.ExecutionContext.IsRunMode)
            {
                requestMonitor = builder.AddProject<UKHO_ADDS_EFS_BuildRequestMonitor>(ProcessNames.RequestMonitorService)
                    .WithReference(storageQueue)
                    .WaitFor(storageQueue)
                    .WithReference(mockService)
                    .WaitFor(mockService)
                    .WithReference(storageBlob)
                    .WaitFor(storageBlob);
            }

            // Orchestrator
            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ProcessNames.OrchestratorService)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WithReference(storageBlob)
                .WaitFor(storageBlob)
                .WithReference(mockService)
                .WaitFor(mockService)
                .WithReference(redisCache)
                .WaitFor(redisCache)
                .WithAzureUserAssignedIdentity(efsServiceIdentity)
                .WithExternalHttpEndpoints()
                .WithScalar("API Browser");

            if (builder.ExecutionContext.IsPublishMode)
            {
                orchestratorService.WithReference(appInsights!);
                orchestratorService.WaitFor(appInsights!);
                orchestratorService.WithReference(eventHubs!);
                orchestratorService.WaitFor(eventHubs!);
                orchestratorService.WithEnvironment("ElasticAPM__ApiKey", elasticApmApiKey!);
                orchestratorService.WithEnvironment("ElasticAPM__ServerURL", elasticApmServerUrl!);
                orchestratorService.WithEnvironment("ElasticAPM__ServiceName", elasticApmServiceName!);
                orchestratorService.WithEnvironment("ElasticAPM__Environment", elasticApmEnvironment!);
                orchestratorService.WithEnvironment("EFS_APP_REG_CLIENT_ID", efsAppRegClientId!);
                orchestratorService.WithEnvironment("EFS_APP_REG_TENANT_ID", efsAppRegTenantId!);
                orchestratorService.WithEnvironment("EFS_B2C_APP_CLIENT_ID", efsB2cAppClientId!);
                orchestratorService.WithEnvironment("EFS_B2C_APP_TENANT_ID", efsB2cAppTenantId!);
                orchestratorService.WithEnvironment("EFS_B2C_APP_DOMAIN", efsB2cAppDomain!);
                orchestratorService.WithEnvironment("EFS_B2C_APP_INSTANCE", efsB2cAppInstance!);
                orchestratorService.WithEnvironment("EFS_B2C_APP_SIGNIN_POLICY", efsB2cAppSigninPolicy!);
                orchestratorService.PublishAsAzureContainerApp((infra, app) =>
                {
                    app.Tags.Add("hidden-title", ServiceConfiguration.ServiceName);
                    var container = app.Template.Containers.Single().Value!;
                    container.Resources.Cpu = orchestratorCpu!.AsProvisioningParameter(infra, "orchestratorCpu");
                    container.Resources.Memory = orchestratorMemory!.AsProvisioningParameter(infra, "orchestratorMemory");
                });
            }

            if (builder.ExecutionContext.IsRunMode)
            {
                orchestratorService.WaitFor(requestMonitor!);
            }

            // Configuration
            if (builder.ExecutionContext.IsRunMode)
            {
                builder.AddConfigurationEmulator(ServiceConfiguration.ServiceName, [orchestratorService, requestMonitor!], [mockService], @"../../configuration/configuration.json", @"../../configuration/external-services.json");
            }
            else
            {
                var appConfig = builder.AddConfiguration(ProcessNames.ConfigurationService, addsEnvironment!, [orchestratorService]).PublishAsExistingWithNullCheck(efsAppConfigurationName, null);
            }

            if (builder.ExecutionContext.IsRunMode)
            {
                var appRootPath = builder.Environment.ContentRootPath;
                await CreateBuilderContainerImages(ProcessNames.S100Builder, "latest", "UKHO.ADDS.EFS.Builder.S100", appRootPath);
                await CreateBuilderContainerImages(ProcessNames.S63Builder, "latest", "UKHO.ADDS.EFS.Builder.S63", appRootPath);
                await CreateBuilderContainerImages(ProcessNames.S57Builder, "latest", "UKHO.ADDS.EFS.Builder.S57", appRootPath);
            }
        }

        private static async Task CreateBuilderContainerImages(string name, string tag, string projectName, string appRootDirectory)
        {
            const int maxBuildAttempts = 3;

            // Check to see if we need to build any images
            var reference = $"{name}:{tag}";

            var dockerClient = new DockerClientConfiguration(GetDockerEndpoint()).CreateClient();

            var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { ["reference"] = new Dictionary<string, bool> { [reference] = true } }
            });

            foreach (var response in images)
            {
                if (response.RepoTags.Any(x => x.Contains(reference)))
                {
                    return;
                }
            }

            Log.Information($"Creating {name} builder container image...");

            var srcDirectory = Directory.GetParent(appRootDirectory)?.FullName!;

            if (name == ProcessNames.S100Builder)
            {
                var workspaceFile = Path.Combine(srcDirectory, projectName, "efs-nonlive-root.tar.gz");
                var imageWorkspaceFile = Path.Combine(srcDirectory, projectName, "root.tar.gz");
                var iicToolFile = Path.Combine(srcDirectory, projectName, "xchg-7.7.war");

                if (File.Exists(workspaceFile) && File.Exists(iicToolFile))
                {
                    File.Copy(workspaceFile, imageWorkspaceFile, true);
                }
                else
                {
                    throw new FileNotFoundException("The IIC Tool files efs-nonlive-root.tar.gz and xchg-7.7.war are missing.");
                }
            }

            var dockerFileName = name == ProcessNames.S100Builder ? "Dockerfile-Release" : "Dockerfile";
            var arguments = $"build -t {name} -f ./{projectName}/{dockerFileName} .";
            for (var attempt = 1; attempt <= maxBuildAttempts; attempt++)
            {
                var standardOutput = new StringBuilder();
                var standardError = new StringBuilder();

                // 'docker' writes everything to stderr...

                var result = await Cli.Wrap("docker")
                    .WithArguments(arguments)
                    .WithWorkingDirectory(srcDirectory)
                    .WithValidation(CommandResultValidation.None)
                    .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                    {
                        Log.Information(line);
                        standardOutput.AppendLine(line);
                    }))
                    .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
                    {
                        Log.Information(line);
                        standardError.AppendLine(line);
                    }))
                    .ExecuteAsync();

                if (result.IsSuccess)
                {
                    Log.Information($"{name} built ok");
                    return;
                }

                var stderrText = standardError.ToString();
                var stdoutText = standardOutput.ToString();
                var retryReason = GetRetryableDockerBuildFailureReason(stderrText, stdoutText);

                if (retryReason is not null && attempt < maxBuildAttempts)
                {
                    var retryDelaySeconds = attempt * 5;
                    Log.Warning("Docker build failed for {Name}. Attempt {Attempt}/{MaxAttempts}. Reason: {RetryReason}. Retrying in {RetryDelaySeconds}s.", name, attempt, maxBuildAttempts, retryReason, retryDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                    continue;
                }

                throw new Exception($"Failed to create {name} builder container image. ExitCode: {result.ExitCode}. Attempt: {attempt}/{maxBuildAttempts}. Command: docker {arguments}. StdErr: {standardError}. StdOut: {standardOutput}");
            }
        }

        private static string? GetRetryableDockerBuildFailureReason(string standardError, string standardOutput)
        {
            var commandOutput = $"{standardError}\n{standardOutput}";

            if (string.IsNullOrWhiteSpace(commandOutput))
            {
                return null;
            }

            if (commandOutput.Contains("unexpected EOF", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains(" EOF", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("failed to copy", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("failed to do request", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("failed to solve", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("failed to compute cache key", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("short read", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("connection reset", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("TLS handshake timeout", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("i/o timeout", StringComparison.OrdinalIgnoreCase)
                || commandOutput.Contains("context canceled", StringComparison.OrdinalIgnoreCase))
            {
                return "transient Docker/network layer download issue";
            }

            return null;
        }

        private static Uri GetDockerEndpoint() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri("npipe://./pipe/docker_engine")
            : new Uri("unix:///var/run/docker.sock");
    }
}

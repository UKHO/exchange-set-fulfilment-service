using CliWrap;
using Microsoft.Extensions.Configuration;
using Projects;
using Serilog;
using UKHO.ADDS.EFS.Common.Configuration.Namespaces;
using UKHO.ADDS.EFS.Common.Configuration.Orchestrator;
using UKHO.ADDS.EFS.LocalHost.Extensions;
using UKHO.ADDS.EFS.LocalHost.OpenTelemetryCollector;

namespace UKHO.ADDS.EFS.LocalHost
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("ADDS EFS Local Host Aspire Orchestrator");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json")
                .Build();

            var mockEndpointPort = config.GetValue<int>("Endpoints:MockEndpointPort");
            var mockEndpointContainerPort = config.GetValue<int>("Endpoints:MockEndpointContainerPort");

            var builderStartup = config.GetValue<BuilderStartup>("Orchestrator:BuilderStartup");

            var exposeOtlp = config.GetValue<bool>("Telemetry:ExposeOtlp");

            var containerRuntime = config.GetValue<ContainerRuntime>("Containers:ContainerRuntime");
            var buildOnStartup = config.GetValue<bool>("Containers:BuildOnStartup");

            var builder = DistributedApplication.CreateBuilder(args);

            // Storage configuration

            var storage = builder.AddAzureStorage(StorageConfiguration.StorageName).RunAsEmulator(e =>
            {
                e.WithDataVolume();
            });

            var storageQueue = storage.AddQueues(StorageConfiguration.QueuesName);
            var storageTable = storage.AddTables(StorageConfiguration.TablesName);

            // ADDS Mock

            var addsMockContainer = builder.AddDockerfile(ContainerConfiguration.MockContainerName, @"..\..\mock\repo\src\ADDSMock")
                .WithHttpEndpoint(mockEndpointPort, mockEndpointContainerPort, ContainerConfiguration.MockContainerEndpointName);

            IResourceBuilder<ContainerResource>? grafanaContainer = null;

            // Metrics
            if (exposeOtlp)
            {
                var prometheusContainer = builder.AddContainer("prometheus", "prom/prometheus:v3.0.1")
                    .WithBindMount("../Metrics/prometheus", "/etc/prometheus", isReadOnly: true)
                    .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
                    .WithHttpEndpoint(targetPort: 9090, name: "http");

                grafanaContainer = builder.AddContainer("grafana", "grafana/grafana")
                    .WithBindMount("../Metrics/grafana/config", "/etc/grafana", isReadOnly: true)
                    .WithBindMount("../Metrics/grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                    .WithEnvironment("PROMETHEUS_ENDPOINT", prometheusContainer.GetEndpoint("http"))
                    .WithHttpEndpoint(targetPort: 3000, name: "http");

                builder.AddOpenTelemetryCollector("otelcollector", "../Metrics/otelcollector/config.yaml")
                    .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheusContainer.GetEndpoint("http")}/api/v1/otlp");
            }

            // Orchestrator

            var orchestratorService = builder.AddProject<UKHO_ADDS_EFS_Orchestrator>(ContainerConfiguration.OrchestratorContainerName)
                .WithReference(storageQueue)
                .WaitFor(storageQueue)
                .WithReference(storageTable)
                .WaitFor(storageTable)
                .WaitFor(addsMockContainer)
                .WithScalar("API documentation");
                

            if (exposeOtlp)
            {
                var grafanaEndpoint = grafanaContainer!.GetEndpoint("http");
                orchestratorService.WithOrchestratorDashboard(grafanaEndpoint, "OLTP Dashboard");
            }

            orchestratorService.WithEnvironment(OrchestratorEnvironmentVariables.BuilderStartup, builderStartup.ToString)
                .WithEnvironment(c =>
                {
                    var addsMockEndpoint = addsMockContainer.GetEndpoint(ContainerConfiguration.MockContainerEndpointName);
                    var fssEndpoint = new UriBuilder(addsMockEndpoint.Url)
                    {
                        Host = addsMockEndpoint.ContainerHost,
                        Path = "fss"
                    };

                    var scsEndpoint = new UriBuilder(addsMockEndpoint.Url)
                    {
                        Host = addsMockEndpoint.ContainerHost,
                        Path = "scs"
                    };

                    var orchestratorServiceEndpoint = orchestratorService.GetEndpoint(name:"http").Url;


                    c.EnvironmentVariables[OrchestratorEnvironmentVariables.FileShareEndpoint] = fssEndpoint.ToString();
                    c.EnvironmentVariables[OrchestratorEnvironmentVariables.SalesCatalogueEndpoint] = scsEndpoint.ToString();
                    c.EnvironmentVariables[OrchestratorEnvironmentVariables.BuildServiceEndpoint] = orchestratorServiceEndpoint.ToString();
                });

            if (buildOnStartup)
            {
                await CreateS100BuilderContainerImage(containerRuntime);
            }

            await builder.Build().RunAsync();

            return 0;
        }

        private static async Task CreateS100BuilderContainerImage(ContainerRuntime containerRuntime)
        {
            Log.Information("Creating S-100 builder container image...");
            Log.Information($"Using container runtime '{containerRuntime}'");

            var localHostDirectory = Directory.GetCurrentDirectory();
            var srcDirectory = Directory.GetParent(localHostDirectory)?.FullName!;

            const string arguments = $"build -t {ContainerConfiguration.S100BuilderContainerName} -f ./UKHO.ADDS.EFS.Builder.S100/Dockerfile .";

            // 'docker' writes everything to stderr...

            var result = await Cli.Wrap(containerRuntime.ToString().ToLowerInvariant())
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

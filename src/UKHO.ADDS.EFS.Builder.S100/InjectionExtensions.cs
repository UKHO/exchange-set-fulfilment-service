using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Extensions;
using UKHO.ADDS.EFS.RetryPolicy;

namespace UKHO.ADDS.EFS.Builder.S100
{
    [ExcludeFromCodeCoverage]
    internal static class InjectionExtensions
    {
        public static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter())
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Azure.Core", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Blobs", LogEventLevel.Fatal)
                .MinimumLevel.Override("Azure.Storage.Queues", LogEventLevel.Warning)
                .CreateLogger();
        }

        public static IConfigurationBuilder AddS100BuilderConfiguration(this IConfigurationBuilder configurationBuilder)
        {
            // Do we have an ADDS Environment set? If not, we are being run manually from Visual Studio. This would normally be set by
            // either Azure or the local BuildRequestMonitor
            var addsEnvironment = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment);

            var isManualRun = string.IsNullOrWhiteSpace(addsEnvironment);

            if (isManualRun)
            {
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment, "local");
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.RequestQueueName, StorageConfiguration.S100BuildRequestQueueName);
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.ResponseQueueName, StorageConfiguration.S100BuildResponseQueueName);
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BlobContainerName, StorageConfiguration.S100JobContainer);

                // Paths are different when running in debug under VS
                var catalinaHomePath = Environment.GetEnvironmentVariable("CATALINA_HOME") ?? string.Empty;
                var currentDirectory = Directory.GetCurrentDirectory();
                var appContextDirectory = AppContext.BaseDirectory;

                var pathPrefix = string.Empty;

                if (currentDirectory.TrimEnd('/').Equals(catalinaHomePath.TrimEnd('/'), StringComparison.InvariantCultureIgnoreCase))
                {
                    pathPrefix = appContextDirectory;
                }

                var portsPath = $"{pathPrefix}ports.json";
                var portsJson = File.ReadAllText(portsPath);

                var ports = GetPorts(portsJson);

                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.QueueConnectionString, $"http://host.docker.internal:{ports.QueuePort}/devstoreaccount1");
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BlobConnectionString, $"http://host.docker.internal:{ports.BlobPort}/devstoreaccount1");
            }

            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder;
        }

        public static IServiceCollection AddS100BuilderServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(ConfigureLogging);
            services.AddHttpClient();
            services.AddStorageClients(configuration);
            services.AddPipelineServices();
            services.AddFileShareServices(configuration);
            services.AddIICToolServices(configuration);

            return services;
        }

        private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            // Clear any default providers
            loggingBuilder.ClearProviders();

            // Add Serilog as the only logger
            loggingBuilder.AddSerilog(dispose: true);
        }

        private static void AddStorageClients(this IServiceCollection services, IConfiguration configuration)
        {

        }

        private static IServiceCollection AddPipelineServices(this IServiceCollection services)
        {
            services.AddSingleton<ExchangeSetPipelineContext>();
            services.AddSingleton<StartupPipeline>();
            services.AddSingleton<AssemblyPipeline>();
            services.AddSingleton<CreationPipeline>();
            services.AddSingleton<DistributionPipeline>();
            services.AddSingleton<INodeStatusWriter, NodeStatusWriter>();

            return services;
        }

        private static IServiceCollection AddFileShareServices(this IServiceCollection services, IConfiguration configuration)
        {
            var fileShareEndpoint = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.FileShareEndpoint)
                ?? configuration["Endpoints:FileShareService"]!;

            // Read-Write Client
            services.AddSingleton<IFileShareReadWriteClientFactory>(provider =>
                new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), string.Empty);
            });

            // Read-Only Client
            services.AddSingleton<IFileShareReadOnlyClientFactory>(provider =>
                new FileShareReadOnlyClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadOnlyClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), string.Empty);
            });

            return services;
        }

        private static IServiceCollection AddIICToolServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IToolClient, ToolClient>((serviceProvider, client) =>
            {
                var baseUrl = configuration["Endpoints:IICTool"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("Endpoints:IICTool configuration is missing.");
                client.BaseAddress = new Uri(baseUrl);
            })

            .AddPolicyHandler((provider, request) =>
             {
                 var loggerFactory = provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
                 var logger = loggerFactory.CreateLogger("Polly.HttpClientRetry");
                 var configuration = provider.GetRequiredService<IConfiguration>();
                 HttpRetryPolicyFactory.SetConfiguration(configuration);
                 return HttpRetryPolicyFactory.GetRetryPolicy(logger);
             });

            return services;
        }

        private static (int QueuePort, int BlobPort) GetPorts(string json)
        {
            var rootNode = JsonNode.Parse(json);
            if (rootNode == null)
            {
                throw new ArgumentException("Invalid JSON: root node is null");
            }

            var queuePortNode = rootNode["QueuePort"];
            var blobPortNode = rootNode["BlobPort"];

            if (queuePortNode == null)
            {
                throw new ArgumentException("JSON does not contain 'QueuePort'");
            }

            if (blobPortNode == null)
            {
                throw new ArgumentException("JSON does not contain 'BlobPort'");
            }

            var queuePort = queuePortNode.GetValue<int>();
            var blobPort = blobPortNode.GetValue<int>();

            return (queuePort, blobPort);
        }
    }
}

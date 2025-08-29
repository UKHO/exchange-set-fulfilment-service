using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builder.Common.Logging;
using UKHO.ADDS.EFS.Builder.S57.Pipelines;
using UKHO.ADDS.EFS.Domain.Extensions;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.S57
{
    [ExcludeFromCodeCoverage]
    internal static class InjectionExtensions
    {
        public static JsonMemorySink ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.JsonMemorySink(new JsonFormatter(), out var sink)
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

            return sink;
        }

        public static IConfigurationBuilder AddBuilderConfiguration(this IConfigurationBuilder configurationBuilder)
        {
            // Do we have an ADDS Environment set? If not, we are being run manually from Visual Studio. This would normally be set by
            // either Azure or the local BuildRequestMonitor
            var addsEnvironment = Environment.GetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment);

            var isManualRun = string.IsNullOrWhiteSpace(addsEnvironment);

            if (isManualRun)
            {
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.AddsEnvironment, "local");
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.RequestQueueName, StorageConfiguration.S57BuildRequestQueueName);
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.ResponseQueueName, StorageConfiguration.S57BuildResponseQueueName);
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BlobContainerName, StorageConfiguration.S57BuildContainer);

                // Paths are different when running in debug under VS
                var catalinaHomePath = Environment.GetEnvironmentVariable("CATALINA_HOME") ?? string.Empty;
                var currentDirectory = Directory.GetCurrentDirectory();
                var appContextDirectory = AppContext.BaseDirectory;

                var pathPrefix = string.Empty;

                if (currentDirectory.TrimEnd('/').Equals(catalinaHomePath.TrimEnd('/'), StringComparison.InvariantCultureIgnoreCase))
                {
                    pathPrefix = appContextDirectory;
                }

                var debugPath = $"{pathPrefix}debug.json";
                var debugDevPath = $"{pathPrefix}debug.Development.json";

                var ports = GetPorts(debugDevPath, debugPath);

                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.QueueConnectionString, $"http://host.docker.internal:{ports.QueuePort}/{QueueClientFactory.AzuriteAccountName}");
                Environment.SetEnvironmentVariable(BuilderEnvironmentVariables.BlobConnectionString, $"http://host.docker.internal:{ports.BlobPort}/{QueueClientFactory.AzuriteAccountName}");

                configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(debugPath)
                    .AddJsonFile(debugDevPath, true);
            }

            configurationBuilder.AddEnvironmentVariables();

            return configurationBuilder;
        }

        public static IServiceCollection AddS57BuilderServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(ConfigureLogging);
            services.AddHttpClient();
            services.AddStorageClients(configuration);
            services.AddPipelineServices();
            services.AddFileShareServices(configuration);

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
            services.AddSingleton<QueueClientFactory>();
            services.AddSingleton<BlobClientFactory>();
        }

        private static IServiceCollection AddPipelineServices(this IServiceCollection services)
        {
            services.AddSingleton<S57ExchangeSetPipelineContext>();
            services.AddSingleton<StartupPipeline>();
            services.AddSingleton<AssemblyPipeline>();
            services.AddSingleton<CreationPipeline>();
            services.AddSingleton<DistributionPipeline>();

            return services;
        }

        private static IServiceCollection AddFileShareServices(this IServiceCollection services, IConfiguration configuration)
        {
            var fileShareEndpoint = configuration[BuilderEnvironmentVariables.FileShareEndpoint] ?? configuration["DebugEndpoints:FileShareService"]!;

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

        private static (int QueuePort, int BlobPort) GetPorts(string debugDevPath, string debugPath)
        {
            var json = string.Empty;

            if (File.Exists(debugDevPath))
            {
                json = File.ReadAllText(debugDevPath);
            }
            else if (File.Exists(debugPath))
            {
                json = File.ReadAllText(debugPath);
            }

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

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadWrite;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Extensions;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UKHO.ADDS.EFS.Builder.S100
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
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

            try
            {
                var provider = ConfigureServices();

                var pipelineContext = provider.GetRequiredService<ExchangeSetPipelineContext>();
                var startupPipeline = provider.GetRequiredService<StartupPipeline>();

                var startupResult = await startupPipeline.ExecutePipeline(pipelineContext);

                if (startupResult.Status != NodeResultStatus.Succeeded)
                {
                    var logger = GetLogger<StartupPipeline>(provider);
                    logger.LogStartupPipelineFailed(startupResult);

                    return BuilderExitCodes.Failed;
                }

                var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                if (assemblyResult.Status != NodeResultStatus.Succeeded)
                {
                    var logger = GetLogger<AssemblyPipeline>(provider);
                    logger.LogAssemblyPipelineFailed(assemblyResult);

                    return BuilderExitCodes.Failed;
                }

                var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                if (creationResult.Status != NodeResultStatus.Succeeded)
                {
                    var logger = GetLogger<CreationPipeline>(provider);
                    logger.LogCreationPipelineFailed(creationResult);

                    return BuilderExitCodes.Failed;
                }

                var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                if (distributionResult.Status != NodeResultStatus.Succeeded)
                {
                    // TODO If the upload stage fails, we should retry?

                    var logger = GetLogger<DistributionPipeline>(provider);
                    logger.LogDistributionPipelineFailed(distributionResult);

                    return BuilderExitCodes.Failed;
                }

                return BuilderExitCodes.Success;
            }
            catch (Exception ex)
            {
#pragma warning disable LOG001
                Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
#pragma warning restore LOG001
                return BuilderExitCodes.Failed;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static ILogger GetLogger<T>(IServiceProvider provider)
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger<T>();
        }

        private static IServiceProvider ConfigureServices()
        {
            var collection = new ServiceCollection();

            var catalinaHomePath = Environment.GetEnvironmentVariable("CATALINA_HOME") ?? string.Empty;

            var currentDirectory = Directory.GetCurrentDirectory();
            var appContextDirectory = AppContext.BaseDirectory;

            var configPathPrefix = string.Empty;

            if (currentDirectory.TrimEnd('/').Equals(catalinaHomePath.TrimEnd('/'), StringComparison.InvariantCultureIgnoreCase))
            {
                configPathPrefix = appContextDirectory;
            }

            var appsettingsPath = $"{configPathPrefix}appsettings.json";
            var appsettingsDevPath = $"{configPathPrefix}appsettings.Development.json";

            collection.AddLogging(loggingBuilder =>
            {
                // Clear any default providers (optional, depends on your needs)
                loggingBuilder.ClearProviders();

                // Add Serilog as the only logger
                loggingBuilder.AddSerilog(dispose: true);
            });

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appsettingsPath)
                .AddJsonFile(appsettingsDevPath, true)
                .Build();
            var fileShareEndpoint = Environment.GetEnvironmentVariable(OrchestratorEnvironmentVariables.FileShareEndpoint)! ?? configuration["Endpoints:FileShareService"]!;

            collection.AddHttpClient();

            collection.AddSingleton<IConfiguration>(x => configuration);
            collection.AddSingleton<ExchangeSetPipelineContext>();
            collection.AddSingleton<StartupPipeline>();
            collection.AddSingleton<AssemblyPipeline>();

            collection.AddSingleton<IFileShareReadWriteClientFactory>(provider =>
               new FileShareReadWriteClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            collection.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadWriteClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), "");
            });

            collection.AddSingleton<IFileShareReadOnlyClientFactory>(provider =>
                new FileShareReadOnlyClientFactory(provider.GetRequiredService<IHttpClientFactory>()));

            collection.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<IFileShareReadOnlyClientFactory>();
                return factory.CreateClient(fileShareEndpoint.RemoveControlCharacters(), "");
            });

            collection.AddSingleton<CreationPipeline>();
            collection.AddSingleton<DistributionPipeline>();

            collection.AddSingleton<INodeStatusWriter, NodeStatusWriter>();
            collection.AddHttpClient<IToolClient, ToolClient>((serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["IICTool:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("IICTool:BaseUrl configuration is missing.");
                client.BaseAddress = new Uri(baseUrl);
            });


            return collection.BuildServiceProvider();
        }
    }
}

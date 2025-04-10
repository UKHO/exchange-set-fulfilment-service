using Serilog;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Configuration.Orchestrator;

namespace UKHO.ADDS.EFS.Builder.S100
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                var provider = ConfigureServices();

                var pipelineContext = provider.GetRequiredService<ExchangeSetPipelineContext>();
                var startupPipeline = provider.GetRequiredService<StartupPipeline>();

                var startupResult = await startupPipeline.ExecutePipeline(pipelineContext);

                if (startupResult.IsFailure(out var startupError))
                {
                    Log.Error($"Startup pipeline failed : {startupError.Message}");
                    return BuilderExitCodes.Failed;
                }

                var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                if (assemblyResult.IsFailure(out var assemblyError))
                {
                    Log.Error($"Assembly pipeline failed : {assemblyError.Message}");
                    return BuilderExitCodes.Failed;
                }

                var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                if (creationResult.IsFailure(out var creationError))
                {
                    Log.Error($"Creation pipeline failed : {creationError.Message}");
                    return BuilderExitCodes.Failed;
                }

                var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                if (distributionResult.IsFailure(out var distributionError))
                {
                    // TODO If the upload stage fails, we should retry?

                    Log.Error($"Distribution pipeline failed : {distributionError.Message}");
                    return BuilderExitCodes.Failed;
                }

                return BuilderExitCodes.Success;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
                return BuilderExitCodes.Failed;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
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

            Log.Information($"Current working directory is {currentDirectory}");
            Log.Information($"Current app context directory is {appContextDirectory}");

            Log.Information($"App settings path is {appsettingsPath}");
            Log.Information($"Dev app settings path is {appsettingsDevPath}");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appsettingsPath)
                .AddJsonFile(appsettingsDevPath, true)
                .Build();

            collection.AddHttpClient();

            collection.AddSingleton<IConfiguration>(x => configuration);

            collection.AddSingleton<ExchangeSetPipelineContext>();
            collection.AddSingleton<StartupPipeline>();
            collection.AddSingleton<AssemblyPipeline>();
            collection.AddSingleton<CreationPipeline>();
            collection.AddSingleton<DistributionPipeline>();

            collection.AddSingleton<INodeStatusWriter, NodeStatusWriter>();
            collection.AddSingleton<IToolClient, ToolClient>();


            return collection.BuildServiceProvider();
        }
    }
}

using Serilog;
using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Services;

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
                    Log.Error($"Startup failed : {startupError.Message}");
                    return -1;
                }

                var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                if (assemblyResult.IsFailure(out var assemblyError))
                {
                    Log.Error($"Assembly failed : {assemblyError.Message}");
                    return -1;
                }

                var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                if (creationResult.IsFailure(out var creationError))
                {
                    Log.Error($"Creation failed : {creationError.Message}");
                    return -1;
                }

                var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                if (distributionResult.IsFailure(out var distributionError))
                {
                    // TODO If the upload stage fails, we should retry?

                    Log.Error($"Distribution failed : {distributionError.Message}");
                    return -1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
                return -1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var collection = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true)
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

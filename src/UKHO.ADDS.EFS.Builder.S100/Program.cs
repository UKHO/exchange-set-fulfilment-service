﻿using System.Diagnostics.CodeAnalysis;
using Serilog;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UKHO.ADDS.EFS.Builder.S100
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            InjectionExtensions.ConfigureSerilog();

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
            var services = new ServiceCollection();

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

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appsettingsPath)
                .AddJsonFile(appsettingsDevPath, true)
                .Build();

            services.AddSingleton<IConfiguration>(x => configuration);

            services.AddS100BuilderServices(configuration);

            return services.BuildServiceProvider();
        }
    }
}

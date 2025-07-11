﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using Serilog;
using UKHO.ADDS.EFS.Builder.S57.Pipelines;
using UKHO.ADDS.EFS.Builder.S57.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S57.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Builder.S57.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Serialization.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UKHO.ADDS.EFS.Builder.S57
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var sink = InjectionExtensions.ConfigureSerilog();
            var exitCode = BuilderExitCode.Success;

            S57ExchangeSetPipelineContext? pipelineContext = null;
            IConfiguration? configuration = null;

            try
            {
                var provider = ConfigureServices();

                pipelineContext = provider.GetRequiredService<S57ExchangeSetPipelineContext>();
                var startupPipeline = provider.GetRequiredService<StartupPipeline>();

                configuration = provider.GetRequiredService<IConfiguration>();

                var startupResult = await startupPipeline.ExecutePipeline(pipelineContext);

                if (startupResult.Status != NodeResultStatus.Succeeded)
                {
                    var logger = GetLogger<StartupPipeline>(provider);
                    logger.LogStartupPipelineFailed(startupResult);

                    exitCode = BuilderExitCode.Failed;
                }

                if (exitCode == BuilderExitCode.Success)
                {
                    var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                    var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                    if (assemblyResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<AssemblyPipeline>(provider);
                        logger.LogAssemblyPipelineFailed(assemblyResult);

                        exitCode = BuilderExitCode.Failed;
                    }
                }

                if (exitCode == BuilderExitCode.Success)
                {
                    var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                    var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                    if (creationResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<CreationPipeline>(provider);
                        logger.LogCreationPipelineFailed(creationResult);

                        exitCode = BuilderExitCode.Failed;
                    }
                }

                if (exitCode == BuilderExitCode.Success)
                {
                    var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                    var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                    if (distributionResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<DistributionPipeline>(provider);
                        logger.LogDistributionPipelineFailed(distributionResult);

                        exitCode = BuilderExitCode.Failed;
                    }
                }

                return (int)exitCode;
            }
            catch (Exception ex)
            {
#pragma warning disable LOG001
                Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
#pragma warning restore LOG001
                return (int)BuilderExitCode.Failed;
            }

            finally
            {
                if (pipelineContext != null && configuration != null)
                {
                    await pipelineContext.CompleteBuild(configuration, sink, exitCode);
                }

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

            var configuration = new ConfigurationBuilder()
                .AddBuilderConfiguration()
                .Build();

            services.AddSingleton<IConfiguration>(x => configuration);

            services.AddS100BuilderServices(configuration);

            return services.BuildServiceProvider();
        }
    }
}

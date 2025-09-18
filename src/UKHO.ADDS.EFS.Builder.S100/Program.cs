using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Context;
using UKHO.ADDS.EFS.Builder.S100.Pipelines;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging;
using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UKHO.ADDS.EFS.Builder.S100
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var sink = InjectionExtensions.ConfigureSerilog();
            var exitCode = BuilderExitCode.Success;

            S100ExchangeSetPipelineContext? pipelineContext = null;
            IConfiguration? configuration = null;

            try
            {
                var provider = ConfigureServices();

                pipelineContext = provider.GetRequiredService<S100ExchangeSetPipelineContext>();
                if (pipelineContext == null)
                {
                    throw new InvalidOperationException($"{nameof(S100ExchangeSetPipelineContext)} is not registered in the service provider.");
                }

                var startupPipeline = provider.GetRequiredService<StartupPipeline>();

                configuration = provider.GetRequiredService<IConfiguration>();

                var startupResult = await startupPipeline.ExecutePipeline(pipelineContext);

                if (startupResult.Status != NodeResultStatus.Succeeded)
                {
                    var logger = GetLogger<StartupPipeline>(provider);

                    var startupNodeResult = await CreateNodeResultLogViewAsync(startupResult);
                    logger.LogStartupPipelineFailed(startupNodeResult);

                    exitCode = BuilderExitCode.Failed;
                }

                if (exitCode != BuilderExitCode.Success)
                {
                    return (int)exitCode;
                }

                // Once we have JobId, establish correlation context for ALL subsequent operations
                using (LogContext.PushProperty(LogProperties.CorrelationId, pipelineContext.JobId))
                {
                    var assemblyPipeline = provider.GetRequiredService<AssemblyPipeline>();
                    var assemblyResult = await assemblyPipeline.ExecutePipeline(pipelineContext);

                    if (assemblyResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<AssemblyPipeline>(provider);
                        var assemblyNodeResult = await CreateNodeResultLogViewAsync(assemblyResult);

                        logger.LogAssemblyPipelineFailed(assemblyNodeResult);

                        exitCode = BuilderExitCode.Failed;
                        return (int)exitCode;
                    }

                    var creationPipeline = provider.GetRequiredService<CreationPipeline>();
                    var creationResult = await creationPipeline.ExecutePipeline(pipelineContext);

                    if (creationResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<CreationPipeline>(provider);
                        var creationNodeResult = await CreateNodeResultLogViewAsync(creationResult);

                        logger.LogCreationPipelineFailed(creationNodeResult);

                        exitCode = BuilderExitCode.Failed;
                        return (int)exitCode;
                    }

                    var distributionPipeline = provider.GetRequiredService<DistributionPipeline>();
                    var distributionResult = await distributionPipeline.ExecutePipeline(pipelineContext);

                    if (distributionResult.Status != NodeResultStatus.Succeeded)
                    {
                        var logger = GetLogger<DistributionPipeline>(provider);
                        var distributionNodeResult = await CreateNodeResultLogViewAsync(distributionResult);

                        logger.LogDistributionPipelineFailed(distributionNodeResult);

                        exitCode = BuilderExitCode.Failed;
                        return (int)exitCode;
                    }
                    return (int)exitCode;
                }
            }
            catch (Exception ex)
            {
                var correlationId = pipelineContext?.JobId.ToString() ?? "unknown";
                using (LogContext.PushProperty(LogProperties.CorrelationId, correlationId))
                {
#pragma warning disable LOG001
                    Log.Fatal(ex, $"An unhandled exception occurred during execution : {ex.Message}");
#pragma warning restore LOG001
                }
                return (int)BuilderExitCode.Failed;
            }
            finally
            {
                // Ensure correlation context for completion
                var correlationId = pipelineContext?.JobId.ToString() ?? "unknown";
                using (LogContext.PushProperty(LogProperties.CorrelationId, correlationId))
                {
                    if (pipelineContext != null)
                    {
                        await pipelineContext.CompleteBuild(configuration, sink, exitCode);
                    }
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

        private static async Task<NodeResultLogView> CreateNodeResultLogViewAsync(NodeResult nodeResult)
        {
            var subject = nodeResult.Subject as S100ExchangeSetPipelineContext;

            var nodeResultLogView = new NodeResultLogView
            {
                BatchId = subject.BatchId,
                BuildNodeStatuses = [.. subject.Statuses],
                Status = nodeResult.Status,
                Exception = nodeResult.Exception,
                ChildResults = []
            };
            foreach (var childNode in nodeResult.ChildResults)
            {
                var childResult = new ChildNodeResultLogView
                {
                    Id = childNode.Id,
                    Status = childNode.Status,
                    Exception = childNode.Exception
                };
                nodeResultLogView.ChildResults.Add(childResult);
            }
            return nodeResultLogView;

        }
    }
}

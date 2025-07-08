using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class CompletionPipelineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CompletionPipelineFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public CompletionPipeline CreateCompletionPipeline(CompletionPipelineContext context) =>
            context.DataStandard switch
            {
                DataStandard.S100 => ActivatorUtilities.CreateInstance<S100CompletionPipeline>(_serviceProvider, context),
                DataStandard.S57 => ActivatorUtilities.CreateInstance<S57CompletionPipeline>(_serviceProvider, context),
                DataStandard.S63 => ActivatorUtilities.CreateInstance<S63CompletionPipeline>(_serviceProvider, context),
                _ => throw new NotSupportedException($"Data standard {context.DataStandard} is not supported.")
            };
    }
}

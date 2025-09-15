using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal class CompletionPipelineFactory : ICompletionPipelineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CompletionPipelineFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public CompletionPipeline CreateCompletionPipeline(CompletionPipelineParameters parameters) =>
            parameters.DataStandard switch
            {
                DataStandard.S100 => ActivatorUtilities.CreateInstance<S100CompletionPipeline>(_serviceProvider, parameters),
                DataStandard.S57 => ActivatorUtilities.CreateInstance<S57CompletionPipeline>(_serviceProvider, parameters),
                DataStandard.S63 => ActivatorUtilities.CreateInstance<S63CompletionPipeline>(_serviceProvider, parameters),
                var _ => throw new NotSupportedException($"Data standard {parameters.DataStandard} is not supported.")
            };
    }
}

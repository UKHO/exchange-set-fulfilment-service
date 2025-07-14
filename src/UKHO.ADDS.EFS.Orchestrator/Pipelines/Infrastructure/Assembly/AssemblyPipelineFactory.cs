using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AssemblyPipelineFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public AssemblyPipeline CreateAssemblyPipeline(AssemblyPipelineParameters parameters) =>
            parameters.DataStandard switch
            {
                DataStandard.S100 => ActivatorUtilities.CreateInstance<S100AssemblyPipeline>(_serviceProvider, parameters),
                DataStandard.S57 => ActivatorUtilities.CreateInstance<S57AssemblyPipeline>(_serviceProvider, parameters),
                DataStandard.S63 => ActivatorUtilities.CreateInstance<S63AssemblyPipeline>(_serviceProvider, parameters),
                _ => throw new NotSupportedException($"Data standard {parameters.DataStandard} is not supported.")
            };
    }
}

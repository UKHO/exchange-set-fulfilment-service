using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class AssemblyPipelineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AssemblyPipelineFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public AssemblyPipeline CreateAssemblyPipeline(AssemblyPipelineParameters parameters) =>
            parameters.DataStandard switch
            {
                ExchangeSetDataStandard.S100 => ActivatorUtilities.CreateInstance<S100AssemblyPipeline>(_serviceProvider, parameters),
                ExchangeSetDataStandard.S57 => ActivatorUtilities.CreateInstance<S57AssemblyPipeline>(_serviceProvider, parameters),
                ExchangeSetDataStandard.S63 => ActivatorUtilities.CreateInstance<S63AssemblyPipeline>(_serviceProvider, parameters),
                _ => throw new NotSupportedException($"Data standard {parameters.DataStandard} is not supported.")
            };
    }
}

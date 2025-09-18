using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineFactory : IAssemblyPipelineFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AssemblyPipelineFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IAssemblyPipeline CreateAssemblyPipeline(AssemblyPipelineParameters parameters) =>
            (parameters.DataStandard, parameters.RequestType) switch
            {
                (DataStandard.S100, RequestType.ProductNames) => ActivatorUtilities.CreateInstance<S100CustomAssemblyPipeline>(_serviceProvider, parameters),
                (DataStandard.S100, RequestType.ProductVersions) => ActivatorUtilities.CreateInstance<S100CustomAssemblyPipeline>(_serviceProvider, parameters),
                (DataStandard.S100, RequestType.UpdatesSince) => ActivatorUtilities.CreateInstance<S100CustomAssemblyPipeline>(_serviceProvider, parameters),
                (DataStandard.S100, _) => ActivatorUtilities.CreateInstance<S100AssemblyPipeline>(_serviceProvider, parameters),
                (DataStandard.S57, _) => ActivatorUtilities.CreateInstance<S57AssemblyPipeline>(_serviceProvider, parameters),
                (DataStandard.S63, _) => ActivatorUtilities.CreateInstance<S63AssemblyPipeline>(_serviceProvider, parameters),
                _ => throw new NotSupportedException($"Data standard {parameters.DataStandard} with request type {parameters.RequestType} is not supported.")
            };
    }
}

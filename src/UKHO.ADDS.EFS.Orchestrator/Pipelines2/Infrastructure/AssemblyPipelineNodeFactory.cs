using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure.Markers;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class AssemblyPipelineNodeFactory 
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public AssemblyPipelineNodeFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public T CreateNode<T>(CancellationToken cancellationToken) where T : IAssemblyPipelineNode
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<T>>();
            var environment = new NodeEnvironment(_configuration, cancellationToken, logger);

            return ActivatorUtilities.CreateInstance<T>(_serviceProvider, environment);
        }
    }
}

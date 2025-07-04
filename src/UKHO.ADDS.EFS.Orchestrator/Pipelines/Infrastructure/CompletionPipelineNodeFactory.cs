using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class CompletionPipelineNodeFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public CompletionPipelineNodeFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public T CreateNode<T>(CancellationToken cancellationToken) where T : ICompletionPipelineNode
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<T>>();
            var environment = new NodeEnvironment(_configuration, cancellationToken, logger);

            return ActivatorUtilities.CreateInstance<T>(_serviceProvider, environment);
        }
    }
}

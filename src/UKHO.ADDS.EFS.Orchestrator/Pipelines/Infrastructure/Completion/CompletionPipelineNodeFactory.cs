using UKHO.ADDS.EFS.Domain.Builds;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal class CompletionPipelineNodeFactory : ICompletionPipelineNodeFactory
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public CompletionPipelineNodeFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public T CreateNode<T>(CancellationToken cancellationToken, BuilderExitCode exitCode) where T : ICompletionPipelineNode
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<T>>();
            var environment = new CompletionNodeEnvironment(_configuration, cancellationToken, logger, exitCode);

            return ActivatorUtilities.CreateInstance<T>(_serviceProvider, environment);
        }
    }
}

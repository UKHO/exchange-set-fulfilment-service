using UKHO.ADDS.EFS.Builds;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal abstract class CompletionPipeline
    {
        public abstract Task RunAsync(CancellationToken cancellationToken);
    }

    internal abstract class CompletionPipeline<TBuild> : CompletionPipeline where TBuild : Build, new()
    {
        private readonly ILogger _logger;
        private readonly CompletionPipelineNodeFactory _nodeFactory;
        private readonly PipelineContextFactory<TBuild> _contextFactory;
        private readonly CompletionPipelineParameters _parameters;

        protected CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<TBuild> contextFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        protected CompletionPipelineParameters Parameters => _parameters;

        protected CompletionPipelineNodeFactory NodeFactory => _nodeFactory;

        protected PipelineContextFactory<TBuild> ContextFactory => _contextFactory;

        protected abstract Task<PipelineContext<TBuild>> CreateContext();
    }
}

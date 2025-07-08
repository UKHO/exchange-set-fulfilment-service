using UKHO.ADDS.EFS.NewEFS;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal abstract class CompletionPipeline
    {
        public abstract Task RunAsync(CancellationToken cancellationToken);
    }

    internal abstract class CompletionPipeline<TBuild> : CompletionPipeline where TBuild : Build
    {
        private readonly ILogger _logger;
        private readonly CompletionPipelineNodeFactory _nodeFactory;
        private readonly CompletionPipelineParameters _parameters;

        protected CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _logger = logger;
        }

        protected CompletionPipelineParameters Parameters => _parameters;

        protected CompletionPipelineNodeFactory NodeFactory => _nodeFactory;

        protected abstract PipelineContext<TBuild> CreateContext();
    }
}

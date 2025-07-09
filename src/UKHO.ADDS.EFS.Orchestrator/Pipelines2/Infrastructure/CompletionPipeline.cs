using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure.Markers;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

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

        private readonly PipelineNode<PipelineContext<TBuild>> _pipeline;

        protected CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<TBuild> contextFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _contextFactory = contextFactory;
            _logger = logger;

            _pipeline = new PipelineNode<PipelineContext<TBuild>>();
        }

        protected CompletionPipelineParameters Parameters => _parameters;

        protected PipelineNode<PipelineContext<TBuild>> Pipeline => _pipeline;

        protected PipelineContextFactory<TBuild> ContextFactory => _contextFactory;

        protected abstract Task<PipelineContext<TBuild>> CreateContext();

        protected void AddPipelineNode<TNode>(CancellationToken cancellationToken) where TNode : ICompletionPipelineNode, INode<PipelineContext<TBuild>>
        {
            var node = _nodeFactory.CreateNode<TNode>(cancellationToken);
            _pipeline?.AddChild(node);
        }
    }
}

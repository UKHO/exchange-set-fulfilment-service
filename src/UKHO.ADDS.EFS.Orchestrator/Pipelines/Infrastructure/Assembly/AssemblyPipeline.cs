using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Contexts;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal abstract class AssemblyPipeline
    {
        public abstract Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken);
    }

    internal abstract class AssemblyPipeline<TBuild> : AssemblyPipeline where TBuild : Build, new()
    {
        private readonly ILogger _logger;
        private readonly AssemblyPipelineNodeFactory _nodeFactory;
        private readonly PipelineContextFactory<TBuild> _contextFactory;
        private readonly AssemblyPipelineParameters _parameters;

        private readonly PipelineNode<PipelineContext<TBuild>> _pipeline;

        protected AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<TBuild> contextFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _contextFactory = contextFactory;
            _logger = logger;

            _pipeline = new PipelineNode<PipelineContext<TBuild>>()
            {
                LocalOptions = new ExecutionOptions()
                {
                    ContinueOnFailure = false,
                    ThrowOnError = false
                }
            };
        }

        protected AssemblyPipelineParameters Parameters => _parameters;

        protected PipelineNode<PipelineContext<TBuild>> Pipeline => _pipeline;

        protected PipelineContextFactory<TBuild> ContextFactory => _contextFactory;

        protected abstract Task<PipelineContext<TBuild>> CreateContext();

        protected void AddPipelineNode<TNode>(CancellationToken cancellationToken) where TNode : IAssemblyPipelineNode, INode<PipelineContext<TBuild>>
        {
            var node = _nodeFactory.CreateNode<TNode>(cancellationToken);
            _pipeline?.AddChild(node);
        }
    }
}

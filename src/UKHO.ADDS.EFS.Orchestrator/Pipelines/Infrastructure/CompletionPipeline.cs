namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal abstract class CompletionPipeline
    {
        private readonly CompletionPipelineContext _context;
        private readonly ILogger _logger;
        private readonly CompletionPipelineNodeFactory _nodeFactory;

        protected CompletionPipeline(CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger logger)
        {
            _context = context;
            _nodeFactory = nodeFactory;
            _logger = logger;
        }

        protected CompletionPipelineContext Context => _context;

        protected CompletionPipelineNodeFactory NodeFactory => _nodeFactory;

        public abstract Task RunAsync(CancellationToken cancellationToken);
    }
}

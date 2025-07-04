using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S57CompletionPipeline : CompletionPipeline
    {
        public S57CompletionPipeline(CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger<S57CompletionPipeline> logger)
            : base(context, nodeFactory, logger)
        {
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
        }
    }
}

using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S63CompletionPipeline : CompletionPipeline
    {
        public S63CompletionPipeline(CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger<S63CompletionPipeline> logger)
            : base(context, nodeFactory, logger)
        {
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
        }
    }
}

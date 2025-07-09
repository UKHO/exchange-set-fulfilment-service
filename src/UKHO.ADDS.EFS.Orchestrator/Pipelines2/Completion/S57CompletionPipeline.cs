using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Completion
{
    internal class S57CompletionPipeline : CompletionPipeline<S57Build>
    {
        public S57CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<S57Build> contextFactory, ILogger logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S57Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

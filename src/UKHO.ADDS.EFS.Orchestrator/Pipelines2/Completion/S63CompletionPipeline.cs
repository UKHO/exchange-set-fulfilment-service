using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Completion
{
    internal class S63CompletionPipeline : CompletionPipeline<S63Build>
    {
        public S63CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<S63Build> contextFactory, ILogger logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S63Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

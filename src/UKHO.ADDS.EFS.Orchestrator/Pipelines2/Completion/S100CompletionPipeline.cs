using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Completion
{
    internal class S100CompletionPipeline : CompletionPipeline<S100Build>
    {
        public S100CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<S100Build> contextFactory, ILogger logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

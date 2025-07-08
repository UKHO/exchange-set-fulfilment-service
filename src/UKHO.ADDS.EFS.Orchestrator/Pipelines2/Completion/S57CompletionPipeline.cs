using UKHO.ADDS.EFS.NewEFS.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Completion
{
    internal class S57CompletionPipeline : CompletionPipeline<S57Build>
    {
        public S57CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, ILogger logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override PipelineContext<S57Build> CreateContext()
        {
            return new PipelineContext<S57Build>();
        }
    }
}

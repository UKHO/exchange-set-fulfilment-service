using UKHO.ADDS.EFS.NewEFS.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Completion
{
    internal class S63CompletionPipeline : CompletionPipeline<S63Build>
    {
        public S63CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, ILogger logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override PipelineContext<S63Build> CreateContext()
        {
            return new PipelineContext<S63Build>();
        }
    }
}

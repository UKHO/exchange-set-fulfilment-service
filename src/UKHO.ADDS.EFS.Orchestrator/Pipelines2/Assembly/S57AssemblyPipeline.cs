using UKHO.ADDS.EFS.NewEFS.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S57AssemblyPipeline : AssemblyPipeline<S57Build>
    {
        public S57AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override PipelineContext<S57Build> CreateContext()
        {
            return new PipelineContext<S57Build>();
        }
    }
}

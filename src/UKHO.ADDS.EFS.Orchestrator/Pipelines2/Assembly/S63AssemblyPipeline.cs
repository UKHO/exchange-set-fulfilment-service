using UKHO.ADDS.EFS.NewEFS.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S63AssemblyPipeline : AssemblyPipeline<S63Build>
    {
        public S63AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override PipelineContext<S63Build> CreateContext()
        {
            return new PipelineContext<S63Build>();
        }
    }
}

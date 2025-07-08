using UKHO.ADDS.EFS.NewEFS.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S100AssemblyPipeline : AssemblyPipeline<S100Build>
    {
        public S100AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override PipelineContext<S100Build> CreateContext()
        {
            return new PipelineContext<S100Build>();
        }
    }
}

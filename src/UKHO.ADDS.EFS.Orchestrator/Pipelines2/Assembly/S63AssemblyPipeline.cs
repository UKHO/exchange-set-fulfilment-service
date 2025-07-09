using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S63AssemblyPipeline : AssemblyPipeline<S63Build>
    {
        public S63AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S63Build> contextFactory, ILogger logger)
            : base(parameters, nodeFactory,  contextFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S63Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

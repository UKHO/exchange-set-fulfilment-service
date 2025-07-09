using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S100AssemblyPipeline : AssemblyPipeline<S100Build>
    {
        public S100AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S100Build> contextFactory, ILogger logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

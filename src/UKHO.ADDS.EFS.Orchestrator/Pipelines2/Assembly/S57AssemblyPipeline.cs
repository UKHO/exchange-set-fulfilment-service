using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S57AssemblyPipeline : AssemblyPipeline<S57Build>
    {
        public S57AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S57Build> contextFactory, ILogger<S57AssemblyPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override async Task<PipelineContext<S57Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

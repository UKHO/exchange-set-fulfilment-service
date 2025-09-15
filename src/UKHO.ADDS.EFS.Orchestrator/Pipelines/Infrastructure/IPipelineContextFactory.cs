using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal interface IPipelineContextFactory<TBuild> where TBuild : Build, new()
    {
        Task Persist(PipelineContext<TBuild> context);

        Task<PipelineContext<TBuild>> CreatePipelineContext(AssemblyPipelineParameters parameters);

        Task<PipelineContext<TBuild>> CreatePipelineContext(CompletionPipelineParameters parameters);
    }
}

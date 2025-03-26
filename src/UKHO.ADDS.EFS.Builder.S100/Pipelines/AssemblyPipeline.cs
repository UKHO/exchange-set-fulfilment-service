using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<PipelineContext>
    {
        public Task<IResult<PipelineContext>> ExecutePipeline(PipelineContext context) => Task.FromResult<IResult<PipelineContext>>(Result.Success(context));
    }
}

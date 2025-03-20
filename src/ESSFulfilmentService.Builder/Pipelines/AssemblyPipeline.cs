using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder.Pipelines
{
    internal class AssemblyPipeline : IBuilderPipeline<AssemblyPipelineContext>
    {
        public Task<IResult<AssemblyPipelineContext>> ExecutePipeline(AssemblyPipelineContext context) => throw new NotImplementedException();
    }
}

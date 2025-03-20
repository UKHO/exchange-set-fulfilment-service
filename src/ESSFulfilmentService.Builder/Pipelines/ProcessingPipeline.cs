using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder.Pipelines
{
    internal class ProcessingPipeline : IBuilderPipeline<ProcessingPipelineContext>
    {
        public Task<IResult<ProcessingPipelineContext>> ExecutePipeline(ProcessingPipelineContext context) => throw new NotImplementedException();
    }
}

using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder.Pipelines
{
    internal class DistributionPipeline : IBuilderPipeline<DistributionPipelineContext>
    {
        public Task<IResult<DistributionPipelineContext>> ExecutePipeline(DistributionPipelineContext context) => throw new NotImplementedException();
    }
}

using UKHO.ADDS.Infrastructure.Results;

namespace ESSFulfilmentService.Builder.Pipelines
{
    public interface IBuilderPipeline<TContext> where TContext : class
    {
        Task<IResult<TContext>> ExecutePipeline(TContext context);
    }
}

using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    public interface IBuilderPipeline<TContext> where TContext : class
    {
        Task<IResult<TContext>> ExecutePipeline(TContext context);
    }
}

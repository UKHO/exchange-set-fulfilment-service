using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    public interface IBuilderPipeline<in TContext> where TContext : class
    {
        Task<NodeResult> ExecutePipeline(TContext context);
    }
}

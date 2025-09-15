using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S63
{
    internal class GetProductsForDataStandardNode : AssemblyPipelineNode<S63Build>
    {
        private readonly IProductService _productService;

        public GetProductsForDataStandardNode(AssemblyNodeEnvironment nodeEnvironment, IProductService productService)
            : base(nodeEnvironment)
        {
            _productService = productService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            build.Products = ["ABCDEF63"];

            await context.Subject.SignalBuildRequired();

            return NodeResultStatus.Succeeded;
        }
    }
}

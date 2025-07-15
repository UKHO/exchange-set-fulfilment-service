using StringToExpression.LanguageDefinitions;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class FilterProductsNode : AssemblyPipelineNode<S100Build>
    {
        private readonly ODataFilterLanguage _language;

        public FilterProductsNode(AssemblyNodeEnvironment nodeEnvironment)
            : base(nodeEnvironment)
        {
            _language = new ODataFilterLanguage();
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            return Task.FromResult(job.JobState == JobState.Created && !string.IsNullOrEmpty(job.RequestedFilter) && build.Products!.Any());
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var predicate = _language.Parse<S100Products>(job.RequestedFilter);
            var existingProducts = build.Products!.AsQueryable();

            var filteredProducts = existingProducts.Where(predicate).ToList();

            if (!filteredProducts.Any())
            {
                await context.Subject.SignalNoBuildRequired();
                return NodeResultStatus.Succeeded;
            }

            build.Products = filteredProducts;
            return NodeResultStatus.Succeeded;
        }
    }
}

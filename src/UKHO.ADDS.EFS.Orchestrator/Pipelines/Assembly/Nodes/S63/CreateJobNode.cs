using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S63
{
    internal class CreateJobNode : AssemblyPipelineNode<S63Build>
    {
        private readonly IStorageService _storageService;

        public CreateJobNode(AssemblyNodeEnvironment nodeEnvironment, IStorageService storageService)
            : base(nodeEnvironment)
        {
            _storageService = storageService;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var result = await _storageService.CreateJobAsync(context.Subject.Job);

            return result.IsSuccess() ? NodeResultStatus.Succeeded : NodeResultStatus.Failed;
        }
    }
}

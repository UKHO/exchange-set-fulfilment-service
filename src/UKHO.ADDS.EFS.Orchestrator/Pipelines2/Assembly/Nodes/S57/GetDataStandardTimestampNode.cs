using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Services;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly.Nodes.S57
{
    internal class GetDataStandardTimestampNode : AssemblyPipelineNode<S57Build>
    {
        private readonly ITimestampService _timestampService;

        public GetDataStandardTimestampNode(NodeEnvironment nodeEnvironment, ITimestampService timestampService)
            : base(nodeEnvironment)
        {
            _timestampService = timestampService;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var timestamp = await _timestampService.GetTimestampForJobAsync(context.Subject.Job);
            context.Subject.Job.DataStandardTimestamp = timestamp;

            return NodeResultStatus.Succeeded;
        }
    }
}

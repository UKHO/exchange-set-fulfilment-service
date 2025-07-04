using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common
{
    internal class GetExistingTimestampNode : AssemblyPipelineNode<ExchangeSetJob>
    {
        private readonly ExchangeSetTimestampTable _timestampTable;

        public GetExistingTimestampNode(NodeEnvironment environment, ExchangeSetTimestampTable timestampTable)
            : base(environment) =>
            _timestampTable = timestampTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetJob> context)
        {
            var timestamp = await _timestampTable.GetTimestampForJobAsync(context.Subject);
            context.Subject.ExistingTimestamp = timestamp;

            return NodeResultStatus.Succeeded;
        }
    }
}

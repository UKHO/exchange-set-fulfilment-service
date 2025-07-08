using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common
{
    internal class GetExistingTimestampNode : AssemblyPipelineNode<ExchangeSetJob>
    {
        private readonly ITable<ExchangeSetTimestamp> _timestampTable;

        public GetExistingTimestampNode(NodeEnvironment environment, ITable<ExchangeSetTimestamp> timestampTable)
            : base(environment) =>
            _timestampTable = timestampTable;

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetJob> context)
        {
            var timestamp = await GetTimestampForJobAsync(context.Subject);
            context.Subject.ExistingTimestamp = timestamp;

            return NodeResultStatus.Succeeded;
        }

        private async Task<DateTime> GetTimestampForJobAsync(ExchangeSetJob job)
        {
            var timestamp = DateTime.MinValue;
            var timestampKey = job.DataStandard.ToString().ToLowerInvariant();

            var timestampResult = await _timestampTable.GetUniqueAsync(timestampKey);

            if (timestampResult.IsSuccess(out var timestampEntity))
            {
                if (timestampEntity.Timestamp.HasValue)
                {
                    timestamp = timestampEntity.Timestamp!.Value;
                }
            }

            return timestamp;
        }
    }
}

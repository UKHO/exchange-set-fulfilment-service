using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common
{
    internal class SetJobTypeNode : AssemblyPipelineNode<ExchangeSetJob>
    {
        private readonly ITable<ExchangeSetJobType> _jobTypeTable;

        public SetJobTypeNode(NodeEnvironment environment, ITable<ExchangeSetJobType> jobTypeTable)
            : base(environment)
        {
            _jobTypeTable = jobTypeTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetJob> context)
        {
            var jobType = new ExchangeSetJobType() { JobId = context.Subject.Id, DataStandard = context.Subject.DataStandard, Timestamp = DateTime.UtcNow};
            await _jobTypeTable.AddAsync(jobType);

            return NodeResultStatus.Succeeded;
        }
    }
}

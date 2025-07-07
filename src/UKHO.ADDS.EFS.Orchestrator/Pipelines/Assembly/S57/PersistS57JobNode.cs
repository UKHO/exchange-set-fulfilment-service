using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S57
{
    internal class PersistS57JobNode : AssemblyPipelineNode<S57ExchangeSetJob>
    {
        private readonly BuildStatusTable _buildStatusTable;
        private readonly S57ExchangeSetJobTable _jobTable;

        public PersistS57JobNode(NodeEnvironment environment, S57ExchangeSetJobTable jobTable, BuildStatusTable buildStatusTable)
            : base(environment)
        {
            _jobTable = jobTable;
            _buildStatusTable = buildStatusTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S57ExchangeSetJob> context)
        {
            var job = context.Subject;

            await _jobTable.AddAsync(job);
            await _buildStatusTable.AddAsync(new BuildStatus { DataStandard = job.DataStandard, ExitCode = BuilderExitCode.NotRun, JobId = job.Id, StartTimestamp = DateTime.UtcNow });

            return NodeResultStatus.Succeeded;
        }
    }
}

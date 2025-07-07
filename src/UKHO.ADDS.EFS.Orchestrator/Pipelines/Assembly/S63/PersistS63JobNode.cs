using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S63
{
    internal class PersistS63JobNode : AssemblyPipelineNode<S63ExchangeSetJob>
    {
        private readonly BuildStatusTable _buildStatusTable;
        private readonly S63ExchangeSetJobTable _jobTable;

        public PersistS63JobNode(NodeEnvironment environment, S63ExchangeSetJobTable jobTable, BuildStatusTable buildStatusTable)
            : base(environment)
        {
            _jobTable = jobTable;
            _buildStatusTable = buildStatusTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S63ExchangeSetJob> context)
        {
            var job = context.Subject;

            await _jobTable.AddAsync(job);
            await _buildStatusTable.AddAsync(new BuildStatus { DataStandard = job.DataStandard, ExitCode = BuilderExitCode.NotRun, JobId = job.Id, StartTimestamp = DateTime.UtcNow });

            return NodeResultStatus.Succeeded;
        }
    }
}

using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.S100
{
    internal class PersistS100JobNode : AssemblyPipelineNode<S100ExchangeSetJob>
    {
        private readonly BuildStatusTable _buildStatusTable;
        private readonly S100ExchangeSetJobTable _jobTable;

        public PersistS100JobNode(NodeEnvironment environment, S100ExchangeSetJobTable jobTable, BuildStatusTable buildStatusTable)
            : base(environment)
        {
            _jobTable = jobTable;
            _buildStatusTable = buildStatusTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<S100ExchangeSetJob> context)
        {
            var job = context.Subject;

            await _jobTable.AddAsync(job);
            await _buildStatusTable.AddAsync(new BuildStatus { DataStandard = job.DataStandard, ExitCode = BuilderExitCode.NotRun, JobId = job.Id, StartTimestamp = DateTime.UtcNow });

            return NodeResultStatus.Succeeded;
        }
    }
}

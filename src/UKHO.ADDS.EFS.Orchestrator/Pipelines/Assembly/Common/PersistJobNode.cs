using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common
{
    internal class PersistJobNode<TJob> : AssemblyPipelineNode<TJob> where TJob : ExchangeSetJob
    {
        private readonly ITable<BuildStatus> _buildStatusTable;
        private readonly ITable<TJob> _jobTable;

        public PersistJobNode(NodeEnvironment environment, ITable<TJob> jobTable, ITable<BuildStatus> buildStatusTable)
            : base(environment)
        {
            _jobTable = jobTable;
            _buildStatusTable = buildStatusTable;
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TJob> context)
        {
            var job = context.Subject;

            await _jobTable.AddAsync(job);
            await _buildStatusTable.AddAsync(new BuildStatus { DataStandard = job.DataStandard, ExitCode = BuilderExitCode.NotRun, JobId = job.Id, StartTimestamp = DateTime.UtcNow });

            return NodeResultStatus.Succeeded;
        }
    }
}

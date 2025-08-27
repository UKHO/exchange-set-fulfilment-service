using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.EFS.VOS;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S63
{
    internal class CommitFileShareBatchNode : CompletionPipelineNode<S63Build>
    {
        private readonly IOrchestratorFileShareClient _fileShareClient;

        public CommitFileShareBatchNode(CompletionNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && Environment.BuilderExitCode == BuilderExitCode.Success);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S63Build>> context)
        {
            var job = context.Subject.Job!;

            var commitBatchResult = await _fileShareClient.CommitBatchAsync((string)job.BatchId, (string)job.GetCorrelationId(), Environment.CancellationToken);

            if (!commitBatchResult.IsSuccess(out _, out _))
            {
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

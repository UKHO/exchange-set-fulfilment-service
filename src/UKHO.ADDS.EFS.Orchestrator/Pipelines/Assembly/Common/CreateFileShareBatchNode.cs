using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Common
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<ExchangeSetJob>
    {
        private readonly FileShareService _fileShareService;

        public CreateFileShareBatchNode(NodeEnvironment environment, FileShareService fileShareService)
            : base(environment) =>
            _fileShareService = fileShareService;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<ExchangeSetJob> context) => Task.FromResult(context.Subject.State != ExchangeSetJobState.Cancelled);

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetJob> context)
        {
            var job = context.Subject;

            var createBatchResponseResult = await _fileShareService.CreateBatchAsync(job.GetCorrelationId(), Environment.CancellationToken);

            if (createBatchResponseResult.IsSuccess(out var batchHandle, out _))
            {
                job.BatchId = batchHandle.BatchId;
                job.State = ExchangeSetJobState.InProgress;
            }
            else
            {
                // Could not create a batch, so the job should fail
                job.State = ExchangeSetJobState.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

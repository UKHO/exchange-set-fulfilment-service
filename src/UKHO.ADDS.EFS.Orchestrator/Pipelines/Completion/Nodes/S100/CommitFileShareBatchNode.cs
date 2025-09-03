using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100
{
    internal class CommitFileShareBatchNode : CompletionPipelineNode<S100Build>
    {
        private readonly IFileService _fileService;

        public CommitFileShareBatchNode(CompletionNodeEnvironment nodeEnvironment, IFileService fileService)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.BatchId != BatchId.None && (Environment.BuilderExitCode == BuilderExitCode.Success || context.Subject.IsErrorFileCreated));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job!;

            try
            {
                var commit = await _fileService.CommitBatchAsync(job.BatchId!, job.GetCorrelationId(), Environment.CancellationToken);
            }
            catch (Exception ex)
            {
                return NodeResultStatus.Failed;
            }
            
            return NodeResultStatus.Succeeded;
        }
    }
}

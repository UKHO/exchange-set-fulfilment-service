using UKHO.ADDS.EFS.Domain.Builds.S57;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.User;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S57
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<S57Build>
    {
        private readonly IFileService _fileService;
        private readonly UserIdentifier _userIdentifier;

        public CreateFileShareBatchNode(AssemblyNodeEnvironment nodeEnvironment, IFileService fileService, UserIdentifier userIdentifier)
            : base(nodeEnvironment)
        {
            _fileService = fileService;
            _userIdentifier = userIdentifier;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            try
            {
                var (batch, _) = await _fileService.CreateBatchAsync(job.GetCorrelationId(), job.ExchangeSetType, _userIdentifier, Environment.CancellationToken);

                job.BatchId = batch.BatchId;
                build.BatchId = batch.BatchId;
            }
            catch (Exception)
            {
                // Could not create a batch, so the job should fail
                await context.Subject.SignalAssemblyError();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

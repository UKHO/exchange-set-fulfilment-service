﻿using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.EFS.Orchestrator.Services.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100
{
    internal class CreateFileShareBatchNode : AssemblyPipelineNode<S100Build>
    {
        private readonly IOrchestratorFileShareClient _fileShareClient;

        public CreateFileShareBatchNode(AssemblyNodeEnvironment nodeEnvironment, IOrchestratorFileShareClient fileShareClient)
            : base(nodeEnvironment)
        {
            _fileShareClient = fileShareClient;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            return Task.FromResult(context.Subject.Job.JobState == JobState.Created);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S100Build>> context)
        {
            var job = context.Subject.Job;
            var build = context.Subject.Build;

            var createBatchResponseResult = await _fileShareClient.CreateBatchAsync(job.GetCorrelationId(), Environment.CancellationToken);

            if (createBatchResponseResult.IsSuccess(out var batchHandle, out _))
            {
                job.BatchId = batchHandle.BatchId;
                build.BatchId = batchHandle.BatchId;
            }
            else
            {
                // Could not create a batch, so the job should fail
                await context.Subject.SignalAssemblyError();
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

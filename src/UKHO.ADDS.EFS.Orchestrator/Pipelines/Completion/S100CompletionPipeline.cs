using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S100CompletionPipeline : CompletionPipeline
    {
        private readonly S100ExchangeSetJobTable _jobTable;

        public S100CompletionPipeline(S100ExchangeSetJobTable jobTable, CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger<S100CompletionPipeline> logger)
            : base(context, nodeFactory, logger) =>
            _jobTable = jobTable;

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO Arrange in parallel

            var pipeline = new PipelineNode<CompletionPipelineContext>();

            pipeline.AddChild(NodeFactory.CreateNode<GetS100BuildSummaryNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetS100JobNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateS100JobNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<ReplayLogsNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<CommitFileShareBatchNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<ExpireOldFileShareBatchesNode>(cancellationToken));

            var result = await pipeline.ExecuteAsync(Context);

            if (Context.Job != null)
            {
                Context.Job.State = result.Status switch
                {
                    NodeResultStatus.Succeeded => ExchangeSetJobState.Succeeded,
                    NodeResultStatus.Failed => ExchangeSetJobState.Failed,
                    _ => Context.Job.State
                };

                await _jobTable.UpdateAsync((S100ExchangeSetJob)Context.Job);
            }
        }
    }
}

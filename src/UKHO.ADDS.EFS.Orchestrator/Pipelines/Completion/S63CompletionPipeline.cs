using UKHO.ADDS.EFS.Builds.S63;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S63;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S63CompletionPipeline : CompletionPipeline
    {
        private readonly ITable<S63ExchangeSetJob> _jobTable;

        public S63CompletionPipeline(ITable<S63ExchangeSetJob> jobTable, CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger<S63CompletionPipeline> logger)
            : base(context, nodeFactory, logger)
        {
            _jobTable = jobTable;
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO Arrange in parallel

            var pipeline = new PipelineNode<CompletionPipelineContext>();

            pipeline.AddChild(NodeFactory.CreateNode<GetBuildSummaryNode<S63BuildSummary>>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetJobNode<S63ExchangeSetJob>>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateJobNode<S63ExchangeSetJob>>(cancellationToken));
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

                await _jobTable.UpdateAsync((S63ExchangeSetJob)Context.Job);
            }

        }
    }
}

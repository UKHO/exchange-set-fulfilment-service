using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Jobs.S57;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Common;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S57CompletionPipeline : CompletionPipeline
    {
        private readonly ITable<S57ExchangeSetJob> _jobTable;

        public S57CompletionPipeline(ITable<S57ExchangeSetJob> jobTable, CompletionPipelineContext context, CompletionPipelineNodeFactory nodeFactory, ILogger<S57CompletionPipeline> logger)
            : base(context, nodeFactory, logger)
        {
            _jobTable = jobTable;
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO Arrange in parallel

            var pipeline = new PipelineNode<CompletionPipelineContext>();

            pipeline.AddChild(NodeFactory.CreateNode<GetBuildSummaryNode<S57BuildSummary>>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<GetJobNode<S57ExchangeSetJob>>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateBuildStatusNode>(cancellationToken));
            pipeline.AddChild(NodeFactory.CreateNode<UpdateJobNode<S57ExchangeSetJob>>(cancellationToken));
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

                await _jobTable.UpdateAsync((S57ExchangeSetJob)Context.Job);
            }

        }
    }
}

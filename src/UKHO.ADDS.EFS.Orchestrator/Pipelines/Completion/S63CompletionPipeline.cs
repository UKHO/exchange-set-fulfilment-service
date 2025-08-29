using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S63;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S63CompletionPipeline : CompletionPipeline<S63Build>
    {
        public S63CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<S63Build> contextFactory, ILogger<S63CompletionPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateBuildMementoNode>(cancellationToken);

            AddPipelineNode<ReplayLogsNode>(cancellationToken);
            AddPipelineNode<CommitFileShareBatchNode>(cancellationToken);
            AddPipelineNode<ExpireFileShareBatchesNode>(cancellationToken);
            AddPipelineNode<CompleteJobNode>(cancellationToken);

            var result = await Pipeline.ExecuteAsync(context);

            switch (result.Status)
            {
                case NodeResultStatus.NotRun:
                case NodeResultStatus.Failed:
                    await context.SignalCompletionFailure();
                    break;
            }
        }

        protected override async Task<PipelineContext<S63Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S57CompletionPipeline : CompletionPipeline<S57Build>
    {
        public S57CompletionPipeline(CompletionPipelineParameters parameters, CompletionPipelineNodeFactory nodeFactory, PipelineContextFactory<S57Build> contextFactory, ILogger<S57CompletionPipeline> logger)
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

        protected override async Task<PipelineContext<S57Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

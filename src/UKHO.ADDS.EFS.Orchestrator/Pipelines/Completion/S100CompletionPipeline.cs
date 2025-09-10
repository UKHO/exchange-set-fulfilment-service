using Serilog.Context;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Constants;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal class S100CompletionPipeline : CompletionPipeline<S100Build>
    {
        public S100CompletionPipeline(CompletionPipelineParameters parameters, ICompletionPipelineNodeFactory nodeFactory, IPipelineContextFactory<S100Build> contextFactory, ILogger<S100CompletionPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override async Task RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateBuildMementoNode>(cancellationToken);

            AddPipelineNode<ReplayLogsNode>(cancellationToken);
            AddPipelineNode<CreateErrorFileNode>(cancellationToken);
            AddPipelineNode<CommitFileShareBatchNode>(cancellationToken);
            AddPipelineNode<ExpireFileShareBatchesNode>(cancellationToken);
            AddPipelineNode<CompleteJobNode>(cancellationToken);

            using (LogContext.PushProperty(LogProperties.CorrelationId, context.Job.GetCorrelationId().ToString()))
            {
                var result = await Pipeline.ExecuteAsync(context);

                switch (result.Status)
                {
                    case NodeResultStatus.NotRun:
                    case NodeResultStatus.Failed:
                        await context.SignalCompletionFailure();
                        break;
                }
            }
        }

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

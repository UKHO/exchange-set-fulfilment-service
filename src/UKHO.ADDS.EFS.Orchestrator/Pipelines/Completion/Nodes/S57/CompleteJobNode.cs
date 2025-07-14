using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion.Nodes.S57
{
    internal class CompleteJobNode : CompletionPipelineNode<S57Build>
    {
        public CompleteJobNode(CompletionNodeEnvironment nodeEnvironment)
            : base(nodeEnvironment)
        {
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            return Task.FromResult(Environment.BuilderExitCode != BuilderExitCode.Failed);
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<PipelineContext<S57Build>> context)
        {
            await context.Subject.SignalCompleted();
            return NodeResultStatus.Succeeded;
        }
    }
}

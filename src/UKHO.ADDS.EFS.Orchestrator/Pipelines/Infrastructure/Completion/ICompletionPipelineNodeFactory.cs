using UKHO.ADDS.EFS.Domain.Builds;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal interface ICompletionPipelineNodeFactory
    {
        T CreateNode<T>(CancellationToken cancellationToken, BuilderExitCode exitCode) where T : ICompletionPipelineNode;
    }
}

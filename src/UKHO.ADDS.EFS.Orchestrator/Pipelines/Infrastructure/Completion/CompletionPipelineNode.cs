using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal class CompletionPipelineNode<TBuild> : Node<PipelineContext<TBuild>>, ICompletionPipelineNode where TBuild : Build
    {
        private readonly CompletionNodeEnvironment _nodeEnvironment;

        protected CompletionPipelineNode(CompletionNodeEnvironment nodeEnvironment)
        {
            _nodeEnvironment = nodeEnvironment;
        }

        protected CompletionNodeEnvironment Environment => _nodeEnvironment;
    }
}

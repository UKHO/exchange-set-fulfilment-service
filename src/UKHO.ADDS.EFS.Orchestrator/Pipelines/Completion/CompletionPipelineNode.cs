using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Completion
{
    internal abstract class CompletionPipelineNode : Node<CompletionPipelineContext>, ICompletionPipelineNode
    {
        private readonly NodeEnvironment _environment;

        protected CompletionPipelineNode(NodeEnvironment environment) => _environment = environment;

        protected NodeEnvironment Environment => _environment;
    }
}

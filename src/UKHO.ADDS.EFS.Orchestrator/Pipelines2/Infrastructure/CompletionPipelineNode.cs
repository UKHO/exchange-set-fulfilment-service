using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure.Markers;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class CompletionPipelineNode<TBuild> : Node<PipelineContext<TBuild>>, IAssemblyPipelineNode where TBuild : Build
    {
        
    }
}

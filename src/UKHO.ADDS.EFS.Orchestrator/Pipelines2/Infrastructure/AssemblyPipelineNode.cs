using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure.Markers;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class AssemblyPipelineNode<TBuild> : Node<PipelineContext<TBuild>>, IAssemblyPipelineNode where TBuild : Build
    {
        private readonly NodeEnvironment _nodeEnvironment;

        protected AssemblyPipelineNode(NodeEnvironment nodeEnvironment)
        {
            _nodeEnvironment = nodeEnvironment;
        }

        protected NodeEnvironment Environment => _nodeEnvironment;
    }
}

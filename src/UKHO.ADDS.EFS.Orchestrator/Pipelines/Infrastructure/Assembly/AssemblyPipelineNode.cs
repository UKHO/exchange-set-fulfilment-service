using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal class AssemblyPipelineNode<TBuild> : Node<PipelineContext<TBuild>>, IAssemblyPipelineNode where TBuild : Build
    {
        private readonly AssemblyNodeEnvironment _nodeEnvironment;

        protected AssemblyPipelineNode(AssemblyNodeEnvironment nodeEnvironment)
        {
            _nodeEnvironment = nodeEnvironment;
        }

        protected AssemblyNodeEnvironment Environment => _nodeEnvironment;
    }
}

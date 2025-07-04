using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal abstract class AssemblyPipelineNode<TJob> : Node<TJob>, IAssemblyPipelineNode where TJob : ExchangeSetJob
    {
        private readonly NodeEnvironment _environment;

        protected AssemblyPipelineNode(NodeEnvironment environment) => _environment = environment;

        protected NodeEnvironment Environment => _environment;
    }
}

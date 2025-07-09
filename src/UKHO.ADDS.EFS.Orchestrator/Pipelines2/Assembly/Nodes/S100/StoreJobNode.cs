using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly.Nodes.S100
{
    internal class StoreJobNode : AssemblyPipelineNode<S100Build>
    {
        public StoreJobNode(NodeEnvironment nodeEnvironment, IStorageService storageService)
            : base(nodeEnvironment)
        {

        }

    }
}

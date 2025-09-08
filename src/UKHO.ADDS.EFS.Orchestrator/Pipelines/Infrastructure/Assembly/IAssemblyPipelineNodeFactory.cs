namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly
{
    internal interface IAssemblyPipelineNodeFactory
    {
        T CreateNode<T>(CancellationToken cancellationToken) where T : IAssemblyPipelineNode;
    }
}

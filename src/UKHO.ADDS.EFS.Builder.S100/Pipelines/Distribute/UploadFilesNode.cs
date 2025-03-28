using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class UploadFilesNode : ExchangeSetPipelineNode
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context) => Task.FromResult(NodeResultStatus.NotRun);
    }
}

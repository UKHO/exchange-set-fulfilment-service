using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    public abstract class ExchangeSetPipelineNode : Node<ExchangeSetPipelineContext>
    {
        protected override void OnAfterExecute(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var result = context.ParentResult;
            var writer = context.Subject.NodeStatusWriter;

            var type = GetType().FullName!;

            var nodeResult = result.ChildResults.LastOrDefault(x => x.Id == type);

            if (nodeResult == null)
            {
                return;
            }

            var status = new ExchangeSetBuilderNodeStatus { JobId = context.Subject.JobId, Sequence = IncrementingCounter.GetNext(), NodeId = type, Status = nodeResult.Status };

            if (result.Exception != null)
            {
                status.ErrorMessage = result.Exception.Message;
            }

            writer.WriteNodeStatusTelemetry(status, context.Subject.BuildServiceEndpoint);
        }
    }
}

using UKHO.ADDS.EFS.Common.Entities;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal abstract class ExchangeSetPipelineNode : Node<ExchangeSetPipelineContext>
    {
        protected override void OnAfterExecute(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var result = context.ParentResult;
            var writer = context.Subject.NodeStatusWriter;

            var type = GetType().FullName!;

            var status = result.ChildResults.LastOrDefault(x => x.Id == type);

            if (status == null)
            {
                return;
            }

            var telemetry = new ExchangeSetBuilderNodeStatus { RequestId = context.Subject.RequestId, Timestamp = $"{Environment.TickCount}{Guid.NewGuid():N}", NodeId = type, Status = status.Status };

            if (result.Exception != null)
            {
                telemetry.ErrorMessage = result.Exception.Message;
            }

            writer.WriteNodeStatusTelemetry(telemetry, context.Subject.BuildServiceEndpoint);
        }
    }
}

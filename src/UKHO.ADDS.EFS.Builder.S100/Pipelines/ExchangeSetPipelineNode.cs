using System.Diagnostics;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal abstract class ExchangeSetPipelineNode : Node<ExchangeSetPipelineContext>
    {
        private readonly Stopwatch _stopwatch;

        protected ExchangeSetPipelineNode()
        {
            _stopwatch = new Stopwatch();
        }

        protected override void OnBeforeExecute(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _stopwatch.Start();
        }

        protected override void OnAfterExecute(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _stopwatch.Stop();

            var result = context.ParentResult;

            var type = GetType().FullName!;

            var nodeResult = result.ChildResults.LastOrDefault(x => x.Id == type);

            if (nodeResult == null)
            {
                return;
            }

            var status = new BuildNodeStatus
            {
                JobId = context.Subject.JobId,
                Sequence = IncrementingCounter.GetNext(),
                NodeId = type,
                Status = nodeResult.Status,
                ElapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds
            };

            if (result.Exception != null)
            {
                status.ErrorMessage = result.Exception.Message;
            }

            context.Subject.Summary.AddStatus(status);
        }
    }
}

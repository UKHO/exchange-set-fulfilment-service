using System.Diagnostics;
using System.Text;
using UKHO.ADDS.EFS.Builder.S100.Services;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines
{
    internal abstract class ExchangeSetPipelineNode : Node<ExchangeSetPipelineContext>
    {
        private readonly Stopwatch _stopwatch;

        protected ExchangeSetPipelineNode() => _stopwatch = new Stopwatch();

        protected override void OnBeforeExecute(IExecutionContext<ExchangeSetPipelineContext> context) => _stopwatch.Start();

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
                Sequence = IncrementingCounter.GetNext(),
                NodeId = GetNodeType(type),
                Status = nodeResult.Status,
                ElapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds,
            };

            if (result.Exception != null)
            {
                status.ErrorMessage = FlattenExceptionMessages(result.Exception);
            }

            if (nodeResult.Exception != null)
            {
                status.ErrorMessage = FlattenExceptionMessages(nodeResult.Exception);
            }

            context.Subject.Summary.AddStatus(status);
        }

        private string GetNodeType(string nodeType)
        {
            var rootNamespace = typeof(Program).Namespace!;
            return nodeType.Replace(rootNamespace, "");
        }

        private static string FlattenExceptionMessages(Exception? ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var level = 0;

            while (ex != null)
            {
                if (level > 0)
                {
                    sb.Append(" --> ");
                }

                sb.Append(ex.GetType().Name);
                sb.Append(": ");
                sb.Append(ex.Message);

                ex = ex.InnerException;
                level++;
            }

            return sb.ToString();
        }
    }
}

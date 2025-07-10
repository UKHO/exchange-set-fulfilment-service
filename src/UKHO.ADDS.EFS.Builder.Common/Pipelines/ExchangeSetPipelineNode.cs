using System.Diagnostics;
using System.Text;
using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.Common.Pipelines
{
    public abstract class ExchangeSetPipelineNode<T, TBuild> : Node<T> where T : ExchangeSetPipelineContext<TBuild> where TBuild : Build 
    {
        private readonly Stopwatch _stopwatch;

        protected ExchangeSetPipelineNode() => _stopwatch = new Stopwatch();

        protected override void OnBeforeExecute(IExecutionContext<T> context) => _stopwatch.Start();

        protected override void OnAfterExecute(IExecutionContext<T> context)
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
                NodeId = GetType().Name,
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

            context.Subject.AddStatus(status);
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

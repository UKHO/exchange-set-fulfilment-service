using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    internal class CreateExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;

        public CreateExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var result = await _toolClient.AddExchangeSetAsync(
                context.Subject.JobId,
                context.Subject.WorkspaceAuthenticationKey,
                context.Subject.Job.CorrelationId
            );

            return result.IsSuccess(out var value, out var error)
                ? NodeResultStatus.Succeeded
                :
                NodeResultStatus.Failed;
        }
    }
}

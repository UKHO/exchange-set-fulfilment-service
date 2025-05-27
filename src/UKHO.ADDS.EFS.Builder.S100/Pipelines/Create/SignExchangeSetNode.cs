using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    internal class SignExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;

        public SignExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentException(nameof(toolClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<SignExchangeSetNode>();

            var result = await _toolClient.SignExchangeSetAsync(context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);

            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogSignExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    internal class AddExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;

        public AddExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddExchangeSetNode>();

            var result = await _toolClient.AddExchangeSetAsync(
                context.Subject.JobId,
                context.Subject.WorkspaceAuthenticationKey,
                context.Subject.Job.CorrelationId
            );

            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogCreateExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }
    }
}

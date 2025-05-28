using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create
{
    internal class AddContentExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;

        public AddContentExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentException(nameof(toolClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var logger = context.Subject.LoggerFactory.CreateLogger<AddContentExchangeSetNode>();

            var result = await _toolClient.AddContentAsync(context.Subject.WorkSpacefssdataPath, context.Subject.JobId, context.Subject.WorkspaceAuthenticationKey, context.Subject.JobId);

            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogAddContentExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }
            return NodeResultStatus.Succeeded;
        }
    }
}

using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.IIC.Models;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

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

            var exchangeSetId = context.Subject.JobId;
            var authKey = context.Subject.WorkspaceAuthenticationKey;
            var correlationId = context.Subject.JobId;
            var resourceLocation = context.Subject.WorkSpaceRootPath;

            IResult<OperationResponse> result = await _toolClient.AddContentAsync(resourceLocation, exchangeSetId, authKey, correlationId);

            if (!result.IsSuccess(out var value, out var error))
            {
                logger.LogAddContentExchangeSetNodeFailed(error);
                return NodeResultStatus.Failed;
            }
            return NodeResultStatus.Succeeded;
        }
    }
}

using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class ExtractExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;
        private ILogger _logger;

        public ExtractExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        protected override async Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            _logger = context.Subject.LoggerFactory.CreateLogger<ExtractExchangeSetNode>();
            try
            {
                return await ExtractExchangeSetAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogExtractExchangeSetNodeFailed(ex.Message);
                return NodeResultStatus.Failed;
            }
        }

        private async Task<NodeResultStatus> ExtractExchangeSetAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            var result = await _toolClient.ExtractExchangeSetAsync("JP8", context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
            //var result = await _toolClient.ExtractExchangeSetAsync(context.Subject.Job?.Id!, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);

            if (result.IsSuccess(out var stream, out var error))
            {
                context.Subject.ExchangeSetStream = stream;
                return NodeResultStatus.Succeeded;             
            }
            else
            {
                LogIICExtractExchangeSetFailed(context, error);
                return NodeResultStatus.Failed;
            }
        }

        private void LogIICExtractExchangeSetFailed(IExecutionContext<ExchangeSetPipelineContext> context, IError error)
        {
            var extractExchangeSetLogView = new ExtractExchangeSetLogView
            {
                ExchangeSetId = context.Subject.Job?.Id!,
                CorrelationId = context.Subject.Job?.CorrelationId ?? string.Empty,
                Error = error?.Message ?? string.Empty
            };

            _logger.LogExtractExchangeSetNodeIICFailed(extractExchangeSetLogView);
        }
    }
}

using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    internal class ExtractExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;
        private ILogger _logger;
        private const string ExchangeSetOutputDirectory = "IicExchangeSetOutput";

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
            //var result = await _toolClient.ExtractExchangeSetAsync("JP8", context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId, DestinationPath);
            var result = await _toolClient.ExtractExchangeSetAsync(context.Subject.Job?.Id!, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job?.CorrelationId!, ExchangeSetOutputDirectory);

            if (result.IsFailure( out var error,out var _))
            {
                _logger.LogExtractExchangeSetNodeFailed(error?.Message!);
                return NodeResultStatus.Failed;
            }

            return NodeResultStatus.Succeeded;
        }        
    }
}

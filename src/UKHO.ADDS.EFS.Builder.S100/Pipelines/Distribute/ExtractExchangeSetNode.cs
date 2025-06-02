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
        private const string ExchangeSetOutputDirectory = "iicExchangeSetOutput";
        private const string ExchangSetOutputPath = "/usr/local/tomcat/ROOT/xchg";

        public ExtractExchangeSetNode(IToolClient toolClient)
        {
            _toolClient = toolClient ?? throw new ArgumentNullException(nameof(toolClient));
        }

        /// <summary>
        /// Executes the node asynchronously within the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing pipeline and job information.</param>
        /// <returns>NodeResultStatus indicating whether the node execution succeeded or failed</returns>
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

        /// <summary>
        /// Extracts the exchange set using the IIC Tool Client.
        /// </summary>
        /// <param name="context">The execution context containing pipeline and job information.</param>
        /// <returns>NodeResultStatus indicating whether the node execution succeeded or failed</returns>
        private async Task<NodeResultStatus> ExtractExchangeSetAsync(IExecutionContext<ExchangeSetPipelineContext> context)
        {
            //TODO:need to remove commented code once dependent PBI code is merged
            //var result = await _toolClient.ExtractExchangeSetAsync("JP8", context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId, DestinationPath);
            var result = await _toolClient.ExtractExchangeSetAsync(context.Subject.Job?.Id!, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job?.CorrelationId!, ExchangeSetOutputDirectory);

            if (result.IsFailure(out var error, out var _))
            {
                _logger.LogExtractExchangeSetNodeFailed(error?.Message!);
                return NodeResultStatus.Failed;
            }
            else
            {
                context.Subject.ExchangeSetFilePath = Path.Combine(ExchangSetOutputPath, ExchangeSetOutputDirectory);
                return NodeResultStatus.Succeeded;
            }
        }
    }
}

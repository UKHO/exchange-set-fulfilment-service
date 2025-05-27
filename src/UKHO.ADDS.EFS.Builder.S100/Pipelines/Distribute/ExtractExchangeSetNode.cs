using UKHO.ADDS.EFS.Builder.S100.IIC;
using UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging;
using UKHO.ADDS.Infrastructure.Pipelines;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute
{
    public class ExtractExchangeSetNode : ExchangeSetPipelineNode
    {
        private readonly IToolClient _toolClient;
        private ILogger _logger;
        private const string WorkspacePath = "/usr/local/tomcat/ROOT/workspaces/working9";

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

            var exchangeSetName = $"S100_ExchangeSet_{DateTime.UtcNow:yyyyMMdd}.zip";
            var fullPath = Path.Combine(WorkspacePath, exchangeSetName);

            //TODO: exchangeSetId need to be passed
            //var result = await _toolClient.ExtractExchangeSetAsync("JP8", context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);
            var result = await _toolClient.ExtractExchangeSetAsync(context.Subject.Job?.Id!, context.Subject.WorkspaceAuthenticationKey, context.Subject.Job.CorrelationId);

            if (result.IsSuccess(out var stream, out var error))
            {
                try
                {
                    SaveFileStreamToFile(stream, fullPath);
                    //TODO: values sets in context for further processing
                    // context.Subject.ExchangeSetStream = stream;
                    context.Subject.ExchangeSetName = exchangeSetName;
                    return NodeResultStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    _logger.LogExtractExchangeSetNodeFailed($"Failed to save exchange set file: {ex.Message}");
                    return NodeResultStatus.Failed;
                }
            }
            else
            {
                LogIICExtractExchangeSetFailed(context, error);
                return NodeResultStatus.Failed;
            }
        }

        private static void SaveFileStreamToFile(Stream fileStream, string fullPath)
        {
            using var fileStreamToWrite = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            fileStream.CopyTo(fileStreamToWrite);
        }

        private void LogIICExtractExchangeSetFailed(IExecutionContext<ExchangeSetPipelineContext> context, IError error)
        {
            var extractExchangeSetLogView = new ExtractExchangeSetLogView
            {
                //TODO: exchangeSetId need to be passed
                ExchangeSetId = context.Subject.Job?.Id!,
                ExchangeSetName = context.Subject.ExchangeSetName,
                CorrelationId = context.Subject.Job?.CorrelationId ?? string.Empty,
                Error = error?.Message ?? string.Empty
            };

            _logger.LogExtractExchangeSetNodeIICFailed(extractExchangeSetLogView);
        }
    }
}

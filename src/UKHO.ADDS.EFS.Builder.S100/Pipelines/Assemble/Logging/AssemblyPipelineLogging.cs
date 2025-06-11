using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;
        private const int CreateBatchNodeFailedId = BaseEventId + 2;
        private const int ProductSearchNodeFailedId = BaseEventId + 3;
        private const int ProductSearchNodeFssSearchFailedId = BaseEventId + 4;
        private const int DownloadFilesNodeFailedId = BaseEventId + 5;
        private const int DownloadFilesNodeFssDownloadFailedId = BaseEventId + 6;
        private const int AddContentExchangeSetNodeFailedId = BaseEventId + 7;
        private const int SignExchangeSetNodeFailedId = BaseEventId + 8;
        private const int AddExchangeSetNodeFailedId = BaseEventId + 9;
        private const int DownloadFilesNodeNoFilesToProcessErrorId = BaseEventId + 10;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        // The Create Batch Node Failed
        public static readonly EventId CreateBatchNodeFailed = new(CreateBatchNodeFailedId, nameof(CreateBatchNodeFailed));

        [LoggerMessage(CreateBatchNodeFailedId, LogLevel.Error, "CreateBatchNode failed: {@error}", EventName = nameof(CreateBatchNodeFailed))]
        public static partial void LogCreateBatchNodeFailed(this ILogger logger, [LogProperties] IError error);

        //The Product Search node failed
        public static readonly EventId ProductSearchNodeFailed = new(ProductSearchNodeFailedId, nameof(ProductSearchNodeFailed));

        [LoggerMessage(ProductSearchNodeFailedId, LogLevel.Error, "ProductSearchNode failed: {@exception}", EventName = nameof(ProductSearchNodeFailed))]
        public static partial void LogProductSearchNodeFailed(this ILogger logger, Exception exception);

        //The Product Search node FSS Search failed
        public static readonly EventId ProductSearchNodeFssSearchFailed = new(ProductSearchNodeFssSearchFailedId, nameof(ProductSearchNodeFssSearchFailed));

        [LoggerMessage(ProductSearchNodeFssSearchFailedId, LogLevel.Error, "ProductSearchNode File Share Service Search failed: {@batchSearchProductsLog}", EventName = nameof(ProductSearchNodeFssSearchFailed))]
        public static partial void LogProductSearchNodeFssSearchFailed(this ILogger logger, BatchProductSearchLog batchSearchProductsLog);

        //The Download Files Node failed
        public static readonly EventId DownloadFilesNodeFailed = new(DownloadFilesNodeFailedId, nameof(DownloadFilesNodeFailed));

        [LoggerMessage(DownloadFilesNodeFailedId, LogLevel.Error, "DownloadFilesNode failed: {@exception}", EventName = nameof(DownloadFilesNodeFailed))]
        public static partial void LogDownloadFilesNodeFailed(this ILogger logger, Exception exception);

        //The Download Files Node failed
        public static readonly EventId DownloadFilesNodeFssDownloadFailed = new(DownloadFilesNodeFssDownloadFailedId, nameof(DownloadFilesNodeFssDownloadFailed));

        [LoggerMessage(DownloadFilesNodeFssDownloadFailedId, LogLevel.Error, "DownloadFilesNode File Share Service Download failed: {@downloadFilesLog}", EventName = nameof(DownloadFilesNodeFssDownloadFailed))]
        public static partial void LogDownloadFilesNodeFssDownloadFailed(this ILogger logger, DownloadFilesLogView downloadFilesLog);

        // The Add Content ExchangeSet Node Failed
        public static readonly EventId AddContentExchangeSetNodeFailed = new(AddContentExchangeSetNodeFailedId, nameof(AddContentExchangeSetNodeFailed));

        [LoggerMessage(AddContentExchangeSetNodeFailedId, LogLevel.Error, "AddContentExchangeSetNode failed: {@error}", EventName = nameof(AddContentExchangeSetNodeFailed))]
        public static partial void LogAddContentExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        // The Sign Exchange Set Node Failed
        public static readonly EventId SignExchangeSetNodeFailed = new(SignExchangeSetNodeFailedId, nameof(SignExchangeSetNodeFailed));

        [LoggerMessage(SignExchangeSetNodeFailedId, LogLevel.Error, "SignExchangeSetNode failed: {@error}", EventName = nameof(SignExchangeSetNodeFailed))]
        public static partial void LogSignExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        // The Create Exchange Set Node Failed
        public static readonly EventId AddExchangeSetNodeFailed = new(AddExchangeSetNodeFailedId, nameof(AddExchangeSetNodeFailed));

        [LoggerMessage(AddExchangeSetNodeFailedId, LogLevel.Error, "AddExchangeSetNode failed: {@error}", EventName = nameof(AddExchangeSetNodeFailed))]
        public static partial void LogAddExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        //The Download Files Node failed
        public static readonly EventId DownloadFilesNodeNoFilesToProcessError = new(DownloadFilesNodeFailedId, nameof(DownloadFilesNodeFailed));

        [LoggerMessage(DownloadFilesNodeNoFilesToProcessErrorId, LogLevel.Error, "DownloadFilesNode failed: {@errorMessage}", EventName = nameof(DownloadFilesNodeNoFilesToProcessError))]
        public static partial void LogDownloadFilesNodeNoFilesToProcessError(this ILogger logger, string errorMessage);
    }
}

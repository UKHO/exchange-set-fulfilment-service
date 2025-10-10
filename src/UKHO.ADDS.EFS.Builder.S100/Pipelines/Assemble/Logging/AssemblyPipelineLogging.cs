using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;
        private const int ProductSearchNodeFailedId = BaseEventId + 2;
        private const int ProductSearchNodeFssSearchFailedId = BaseEventId + 3;
        private const int DownloadFilesNodeFailedId = BaseEventId + 4;
        private const int DownloadFilesNodeFssDownloadFailedId = BaseEventId + 5;
        private const int DownloadFilesNodeNoFilesToProcessErrorId = BaseEventId + 6;
        private const int ZipExtractionFailedId = BaseEventId + 7;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResultLogView result);

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

        //The Download Files Node failed
        public static readonly EventId DownloadFilesNodeNoFilesToProcessError = new(DownloadFilesNodeNoFilesToProcessErrorId, nameof(DownloadFilesNodeNoFilesToProcessError));

        [LoggerMessage(DownloadFilesNodeNoFilesToProcessErrorId, LogLevel.Error, "DownloadFilesNode failed: {@errorMessage}", EventName = nameof(DownloadFilesNodeNoFilesToProcessError))]
        public static partial void LogDownloadFilesNodeNoFilesToProcessError(this ILogger logger, string errorMessage);
        
        // ZIP extraction failed
        public static readonly EventId ZipExtractionFailed = new(ZipExtractionFailedId, nameof(ZipExtractionFailed));
        
        [LoggerMessage(ZipExtractionFailedId, LogLevel.Error, "ZIP extraction failed: {@zipExtractionError}", EventName = nameof(ZipExtractionFailed))]
        public static partial void LogZipExtractionFailed(this ILogger logger, [LogProperties] ZipExtractionErrorLogView zipExtractionError);
    }
}

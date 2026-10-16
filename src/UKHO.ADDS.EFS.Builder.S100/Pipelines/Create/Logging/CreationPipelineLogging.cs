using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class CreationPipelineLogging
    {
        private const int BaseEventId = 3000;

        private const int CreationPipelineFailedId = BaseEventId + 1;
        private const int AddContentExchangeSetNodeFailedId = BaseEventId + 2;
        private const int SignExchangeSetNodeFailedId = BaseEventId + 3;
        private const int AddExchangeSetNodeFailedId = BaseEventId + 4;
        private const int AddContentExchangeSetNodeStartedId = BaseEventId + 5;
        private const int AddContentExchangeSetNodeNoCatalogPathsId = BaseEventId + 6;
        private const int AddContentExchangeSetNodeCatalogPathCountId = BaseEventId + 7;
        private const int AddContentExchangeSetNodeAddingCatalogPathId = BaseEventId + 8;
        private const int AddContentExchangeSetNodeCatalogPathAddFailedId = BaseEventId + 9;
        private const int AddContentExchangeSetNodeCompletedId = BaseEventId + 10;
        private const int AddContentExchangeSetNodeResolvingCatalogPathsId = BaseEventId + 11;
        private const int AddContentExchangeSetNodeBatchFolderMissingId = BaseEventId + 12;
        private const int AddContentExchangeSetNodeCatalogMissingId = BaseEventId + 13;
        private const int AddContentExchangeSetNodeAddContentResponseId = BaseEventId + 14;

        // The creation pipeline failed
        public static readonly EventId CreationPipelineFailed = new(CreationPipelineFailedId, nameof(CreationPipelineFailed));

        [LoggerMessage(CreationPipelineFailedId, LogLevel.Error, "Creation pipeline failed: {@result}", EventName = nameof(CreationPipelineFailed))]
        public static partial void LogCreationPipelineFailed(this ILogger logger, [LogProperties] NodeResultLogView result);

        // The Add Content ExchangeSet Node Failed
        public static readonly EventId AddContentExchangeSetNodeFailed = new(AddContentExchangeSetNodeFailedId, nameof(AddContentExchangeSetNodeFailed));

        [LoggerMessage(AddContentExchangeSetNodeFailedId, LogLevel.Error, "AddContentExchangeSetNode failed: {@error}", EventName = nameof(AddContentExchangeSetNodeFailed))]
        public static partial void LogAddContentExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        public static void LogAddContentExchangeSetNodeStartedDiagnostic(this ILogger logger, string jobId, string downloadPath)
        {
            logger.LogInformation(new EventId(AddContentExchangeSetNodeStartedId, nameof(LogAddContentExchangeSetNodeStartedDiagnostic)),
                "AddContentExchangeSetNode started for JobId {JobId}. Download root path: {DownloadPath}", jobId, downloadPath);
        }

        public static void LogAddContentExchangeSetNodeNoCatalogPathsDiagnostic(this ILogger logger, string jobId)
        {
            logger.LogWarning(new EventId(AddContentExchangeSetNodeNoCatalogPathsId, nameof(LogAddContentExchangeSetNodeNoCatalogPathsDiagnostic)),
                "AddContentExchangeSetNode found no catalog paths to add for JobId {JobId}. Exchange set may be empty.", jobId);
        }

        public static void LogAddContentExchangeSetNodeCatalogPathCountDiagnostic(this ILogger logger, int catalogPathCount, string jobId)
        {
            logger.LogInformation(new EventId(AddContentExchangeSetNodeCatalogPathCountId, nameof(LogAddContentExchangeSetNodeCatalogPathCountDiagnostic)),
                "AddContentExchangeSetNode resolved {CatalogPathCount} catalog path(s) for JobId {JobId}.", catalogPathCount, jobId);
        }

        public static void LogAddContentExchangeSetNodeAddingCatalogPathDiagnostic(this ILogger logger, string catalogPath, string jobId)
        {
            logger.LogInformation(new EventId(AddContentExchangeSetNodeAddingCatalogPathId, nameof(LogAddContentExchangeSetNodeAddingCatalogPathDiagnostic)),
                "AddContentExchangeSetNode adding content from path {CatalogPath} for JobId {JobId}", catalogPath, jobId);
        }

        public static void LogAddContentExchangeSetNodeCatalogPathAddFailedDiagnostic(this ILogger logger, string catalogPath, string jobId)
        {
            logger.LogError(new EventId(AddContentExchangeSetNodeCatalogPathAddFailedId, nameof(LogAddContentExchangeSetNodeCatalogPathAddFailedDiagnostic)),
                "AddContentExchangeSetNode failed while adding content from path {CatalogPath} for JobId {JobId}", catalogPath, jobId);
        }

        public static void LogAddContentExchangeSetNodeCompletedDiagnostic(this ILogger logger, string jobId, int successfulAdds, int catalogPathCount)
        {
            logger.LogInformation(new EventId(AddContentExchangeSetNodeCompletedId, nameof(LogAddContentExchangeSetNodeCompletedDiagnostic)),
                "AddContentExchangeSetNode completed for JobId {JobId}. Successfully added {SuccessfulAdds} of {CatalogPathCount} catalog path(s).", jobId, successfulAdds, catalogPathCount);
        }

        public static void LogAddContentExchangeSetNodeResolvingCatalogPathsDiagnostic(this ILogger logger, int batchFolderCount, string downloadPath)
        {
            logger.LogInformation(new EventId(AddContentExchangeSetNodeResolvingCatalogPathsId, nameof(LogAddContentExchangeSetNodeResolvingCatalogPathsDiagnostic)),
                "Resolving catalog paths for {BatchFolderCount} batch folder(s) from {DownloadPath}", batchFolderCount, downloadPath);
        }

        public static void LogAddContentExchangeSetNodeBatchFolderMissingDiagnostic(this ILogger logger, string batchFolderPath)
        {
            logger.LogWarning(new EventId(AddContentExchangeSetNodeBatchFolderMissingId, nameof(LogAddContentExchangeSetNodeBatchFolderMissingDiagnostic)),
                "Batch folder not found for add content. Expected path: {BatchFolderPath}. Returning no catalog paths.", batchFolderPath);
        }

        public static void LogAddContentExchangeSetNodeCatalogMissingDiagnostic(this ILogger logger, string catalogFilePath, string catalogPath)
        {
            logger.LogWarning(new EventId(AddContentExchangeSetNodeCatalogMissingId, nameof(LogAddContentExchangeSetNodeCatalogMissingDiagnostic)),
                "Catalog file not found at expected path {CatalogFilePath}. AddContent will still be attempted with relative path {CatalogPath}", catalogFilePath, catalogPath);
        }

        public static void LogAddContentExchangeSetNodeAddContentResponseDiagnostic(this ILogger logger, string catalogPath, int responseCode, string responseType, string responseMessage)
        {
            logger.LogInformation(
                new EventId(AddContentExchangeSetNodeAddContentResponseId, nameof(LogAddContentExchangeSetNodeAddContentResponseDiagnostic)),
                "AddContentExchangeSetNode AddContent response for path {CatalogPath}: Code={ResponseCode}, Type={ResponseType}, Message={ResponseMessage}",
                catalogPath,
                responseCode,
                responseType,
                responseMessage);
        }

        // The Sign Exchange Set Node Failed
        public static readonly EventId SignExchangeSetNodeFailed = new(SignExchangeSetNodeFailedId, nameof(SignExchangeSetNodeFailed));

        [LoggerMessage(SignExchangeSetNodeFailedId, LogLevel.Error, "SignExchangeSetNode failed: {@error}", EventName = nameof(SignExchangeSetNodeFailed))]
        public static partial void LogSignExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        // The Create Exchange Set Node Failed
        public static readonly EventId AddExchangeSetNodeFailed = new(AddExchangeSetNodeFailedId, nameof(AddExchangeSetNodeFailed));

        [LoggerMessage(AddExchangeSetNodeFailedId, LogLevel.Error, "AddExchangeSetNode failed: {@error}", EventName = nameof(AddExchangeSetNodeFailed))]
        public static partial void LogAddExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

    }
}

using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Infrastructure.Logging.Services;

namespace UKHO.ADDS.EFS.Infrastructure.Logging
{
    internal static partial class InfrastructureLogging
    {
        private const int BaseEventId = 12000;

        private const int SalesCatalogueErrorId = BaseEventId + 9;
        private const int SalesCatalogueUnexpectedStatusCodeId = BaseEventId + 19;

        private const int SearchCommittedBatchesFailedId = BaseEventId + 20;
        private const int FileShareServiceOperationFailedId = BaseEventId + 21;

        // Callback notification events
        private const int CallbackNotificationSkippedId = BaseEventId + 22;
        private const int CallbackNotificationSuccessId = BaseEventId + 23;
        private const int CallbackNotificationFailedId = BaseEventId + 24;
        private const int CallbackNotificationErrorId = BaseEventId + 25;

        // SCS has returned an error
        public static readonly EventId SalesCatalogueError = new(SalesCatalogueErrorId, nameof(SalesCatalogueError));

        // SCS has returned an Unexpected status Code
        public static readonly EventId SalesCatalogueUnexpectedStatusCode = new(SalesCatalogueUnexpectedStatusCodeId, nameof(SalesCatalogueUnexpectedStatusCode));

        // File Share Service operation failed
        public static readonly EventId FileShareServiceOperationFailed = new(FileShareServiceOperationFailedId, nameof(FileShareServiceOperationFailed));

        // Search committed batches failed
        public static readonly EventId SearchCommittedBatchesFailed = new(SearchCommittedBatchesFailedId, nameof(SearchCommittedBatchesFailed));

        // Callback notification events - exposed for use by CallbackNotificationService
        public static readonly EventId CallbackNotificationSkipped = new(CallbackNotificationSkippedId, nameof(CallbackNotificationSkipped));
        public static readonly EventId CallbackNotificationSuccess = new(CallbackNotificationSuccessId, nameof(CallbackNotificationSuccess));
        public static readonly EventId CallbackNotificationFailed = new(CallbackNotificationFailedId, nameof(CallbackNotificationFailed));
        public static readonly EventId CallbackNotificationError = new(CallbackNotificationErrorId, nameof(CallbackNotificationError));

        [LoggerMessage(SalesCatalogueErrorId, LogLevel.Error, "Sales Catalogue error: {@message}", EventName = nameof(SalesCatalogueError))]
        public static partial void LogSalesCatalogueApiError(this ILogger logger, [LogProperties] SalesCatalogApiErrorLogView message);

        [LoggerMessage(SalesCatalogueUnexpectedStatusCodeId, LogLevel.Error, "Sales Catalogue Unexpected Status Code: {@salesCatalogueLog}", EventName = nameof(SalesCatalogueUnexpectedStatusCode))]
        public static partial void LogUnexpectedSalesCatalogueStatusCode(this ILogger logger, SalesCatalogUnexpectedStatusLogView salesCatalogueLog);

        [LoggerMessage(FileShareServiceOperationFailedId, LogLevel.Error, "File Share Service operation failed: {@fileShareServiceLog}", EventName = nameof(FileShareServiceOperationFailed))]
        public static partial void LogFileShareError(this ILogger logger, [LogProperties] FileShareServiceLogView fileShareServiceLog);

        [LoggerMessage(SearchCommittedBatchesFailedId, LogLevel.Error, "Search committed batches failed: {@searchCommittedBatchesLogView}", EventName = nameof(SearchCommittedBatchesFailed))]
        public static partial void LogFileShareSearchCommittedBatchesError(this ILogger logger, [LogProperties] SearchCommittedBatchesLogView searchCommittedBatchesLogView);

        // Callback notification logging helper methods - these use ILogger.Log directly since LoggerMessage source generation has issues in this environment
        public static void LogCallbackNotificationSkipped(this ILogger logger, CallbackNotificationLogView callbackLogView)
        {
            logger.Log(LogLevel.Information, CallbackNotificationSkipped, "Callback notification skipped: {@CallbackLogView}", callbackLogView);
        }

        public static void LogCallbackNotificationSuccess(this ILogger logger, CallbackNotificationLogView callbackLogView)
        {
            logger.Log(LogLevel.Information, CallbackNotificationSuccess, "Callback notification successful: {@CallbackLogView}", callbackLogView);
        }

        public static void LogCallbackNotificationFailed(this ILogger logger, CallbackNotificationLogView callbackLogView)
        {
            logger.Log(LogLevel.Error, CallbackNotificationFailed, "Callback notification failed: {@CallbackLogView}", callbackLogView);
        }

        public static void LogCallbackNotificationError(this ILogger logger, CallbackNotificationLogView callbackLogView, Exception exception)
        {
            logger.Log(LogLevel.Error, CallbackNotificationError, exception, "Callback notification error: {@CallbackLogView}", callbackLogView);
        }

        public static void TestLog(this ILogger logger, string messesge)
        {
            logger.Log(LogLevel.Error, CallbackNotificationError, "Test Error- : " + messesge);
        }
    }
}

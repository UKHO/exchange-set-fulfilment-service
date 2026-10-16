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
        private const int SalesCatalogueRequestCountId = BaseEventId + 26;
        private const int SalesCatalogueResponseCountId = BaseEventId + 27;
        private const int SalesCatalogueCatalogueCountId = BaseEventId + 28;
        private const int SalesCatalogueResponseSummaryId = BaseEventId + 29;

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
        public static readonly EventId SalesCatalogueRequestCount = new(SalesCatalogueRequestCountId, nameof(SalesCatalogueRequestCount));
        public static readonly EventId SalesCatalogueResponseCount = new(SalesCatalogueResponseCountId, nameof(SalesCatalogueResponseCount));
        public static readonly EventId SalesCatalogueCatalogueCount = new(SalesCatalogueCatalogueCountId, nameof(SalesCatalogueCatalogueCount));
        public static readonly EventId SalesCatalogueResponseSummary = new(SalesCatalogueResponseSummaryId, nameof(SalesCatalogueResponseSummary));

        [LoggerMessage(SalesCatalogueErrorId, LogLevel.Error, "Sales Catalogue error: {@message}", EventName = nameof(SalesCatalogueError))]
        public static partial void LogSalesCatalogueApiError(this ILogger logger, [LogProperties] SalesCatalogApiErrorLogView message);

        [LoggerMessage(SalesCatalogueUnexpectedStatusCodeId, LogLevel.Error, "Sales Catalogue Unexpected Status Code: {@salesCatalogueLog}", EventName = nameof(SalesCatalogueUnexpectedStatusCode))]
        public static partial void LogUnexpectedSalesCatalogueStatusCode(this ILogger logger, [LogProperties] SalesCatalogUnexpectedStatusLogView salesCatalogueLog);

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

        public static void LogSalesCatalogueRequestCount(this ILogger logger, string endpoint, int requestCount, string correlationId)
        {
            logger.Log(
                LogLevel.Information,
                SalesCatalogueRequestCount,
                "Sales Catalogue request count for {Endpoint}: {RequestCount}, CorrelationId: {CorrelationId}",
                endpoint,
                requestCount,
                correlationId);
        }

        public static void LogSalesCatalogueResponseCount(this ILogger logger, string endpoint, int responseProductsCount, int returnedProductCount, string correlationId)
        {
            logger.Log(
                LogLevel.Information,
                SalesCatalogueResponseCount,
                "Sales Catalogue response count for {Endpoint}: Products={ResponseProductsCount}, ReturnedProductCount={ReturnedProductCount}, CorrelationId: {CorrelationId}",
                endpoint,
                responseProductsCount,
                returnedProductCount,
                correlationId);
        }

        public static void LogSalesCatalogueCatalogueCount(this ILogger logger, int catalogueCount, string correlationId)
        {
            logger.Log(
                LogLevel.Information,
                SalesCatalogueCatalogueCount,
                "Sales Catalogue basic catalogue response count: {CatalogueCount}, CorrelationId: {CorrelationId}",
                catalogueCount,
                correlationId);
        }

        public static void LogSalesCatalogueResponseSummary(this ILogger logger, string endpoint, int requestedProductCount, int returnedProductCount, int alreadyUpToDateCount, int notReturnedCount, int productsCount, string correlationId)
        {
            logger.Log(
                LogLevel.Information,
                SalesCatalogueResponseSummary,
                "Sales Catalogue response summary for {Endpoint}: Requested={RequestedProductCount}, Returned={ReturnedProductCount}, AlreadyUpToDate={AlreadyUpToDateCount}, NotReturned={NotReturnedCount}, Products={ProductsCount}, CorrelationId: {CorrelationId}",
                endpoint,
                requestedProductCount,
                returnedProductCount,
                alreadyUpToDateCount,
                notReturnedCount,
                productsCount,
                correlationId);
        }
    }
}

using Microsoft.Extensions.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Infrastructure.Logging
{
    internal static partial class InfrastructureLogging
    {
        private const int BaseEventId = 12000;

        private const int SalesCatalogueErrorId = BaseEventId + 9;
        private const int SalesCatalogueUnexpectedStatusCodeId = BaseEventId + 19;

        private const int SearchCommittedBatchesFailedId = BaseEventId + 20;
        private const int FileShareServiceOperationFailedId = BaseEventId + 21;


        // SCS has returned an error
        public static readonly EventId SalesCatalogueError = new(SalesCatalogueErrorId, nameof(SalesCatalogueError));

        // SCS has returned an Unexpected status Code
        public static readonly EventId SalesCatalogueUnexpectedStatusCode = new(SalesCatalogueUnexpectedStatusCodeId, nameof(SalesCatalogueUnexpectedStatusCode));

        // File Share Service operation failed
        public static readonly EventId FileShareServiceOperationFailed = new(FileShareServiceOperationFailedId, nameof(FileShareServiceOperationFailed));

        // Search committed batches failed
        public static readonly EventId SearchCommittedBatchesFailed = new(SearchCommittedBatchesFailedId, nameof(SearchCommittedBatchesFailed));


        [LoggerMessage(SalesCatalogueErrorId, LogLevel.Error, "Sales Catalogue error: {@message}", EventName = nameof(SalesCatalogueError))]
        public static partial void LogSalesCatalogueApiError(this ILogger logger, [LogProperties] SalesCatalogApiErrorLogView message);

        [LoggerMessage(SalesCatalogueUnexpectedStatusCodeId, LogLevel.Error, "Sales Catalogue Unexpected Status Code: {@salesCatalogueLog}", EventName = nameof(SalesCatalogueUnexpectedStatusCode))]
        public static partial void LogUnexpectedSalesCatalogueStatusCode(this ILogger logger, SalesCatalogUnexpectedStatusLogView salesCatalogueLog);

        [LoggerMessage(FileShareServiceOperationFailedId, LogLevel.Error, "File Share Service operation failed: {@fileShareServiceLog}", EventName = nameof(FileShareServiceOperationFailed))]
        public static partial void LogFileShareError(this ILogger logger, [LogProperties] FileShareServiceLogView fileShareServiceLog);

        [LoggerMessage(SearchCommittedBatchesFailedId, LogLevel.Error, "Search committed batches failed: {@searchCommittedBatchesLogView}", EventName = nameof(SearchCommittedBatchesFailed))]
        public static partial void LogFileShareSearchCommittedBatchesError(this ILogger logger, [LogProperties] SearchCommittedBatchesLogView searchCommittedBatchesLogView);
    }
}

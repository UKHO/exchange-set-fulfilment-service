using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    internal static partial class OrchestratorLogs
    {
        private const int BaseEventId = 10000;

        private const int UnhandledHttpErrorId = BaseEventId + 1;

        private const int PostedToExchangeSetQueueId = BaseEventId + 2;
        private const int PostedToExchangeSetQueueFailedId = BaseEventId + 3;

        private const int PostedStatusUpdateFromBuilderFailedId = BaseEventId + 4;

        private const int GetJobRequestFailedId = BaseEventId + 5;

        private const int JobCreatedId = BaseEventId + 6;
        private const int JobUpdatedId = BaseEventId + 7;
        private const int JobCompletedId = BaseEventId + 8;

        private const int SalesCatalogueErrorId = BaseEventId + 9;

        private const int ContainerExecutionFailedId = BaseEventId + 10;
        private const int ContainerTimeoutId = BaseEventId + 11;
        private const int ContainerWaitFailedId = BaseEventId + 12;
        private const int ContainerStartFailedId = BaseEventId + 13;
        private const int ContainerRemovedId = BaseEventId + 14;


        private const int JobCreationFailedId = BaseEventId + 15;

        private const int QueueServiceMessageReadFailedId = BaseEventId + 16;

        private const int LogForwardParseFailedId = BaseEventId + 17;
        private const int LogForwardParseNullId = BaseEventId + 18;

        private const int SalesCatalogueUnexpectedStatusCodeId = BaseEventId + 19;
        private const int SearchCommittedBatchesFailedId = BaseEventId + 20;
        private const int FileShareServiceOperationFailedId = BaseEventId + 21;

        // An unhandled HTTP error has occurred
        public static readonly EventId UnhandledHttpError = new(UnhandledHttpErrorId, nameof(UnhandledHttpError));

        [LoggerMessage(UnhandledHttpErrorId, LogLevel.Error, "An unhandled exception was caught by the HTTP pipeline: {@message}", EventName = nameof(UnhandledHttpError))]
        public static partial void LogUnhandledHttpError(this ILogger logger, string message, Exception exception);


        // A request has been posted to the Orchestrator queue
        public static readonly EventId PostedExchangeSetQueueMessage = new(PostedToExchangeSetQueueId, nameof(PostedExchangeSetQueueMessage));

        [LoggerMessage(PostedToExchangeSetQueueId, LogLevel.Information, "Posted to EFS queue: {@message}", EventName = nameof(PostedExchangeSetQueueMessage))]
        public static partial void LogPostedExchangeSetQueueMessage(this ILogger logger, [LogProperties] ExchangeSetRequestQueueMessage message);


        // A request post to the Orchestrator queue has failed
        public static readonly EventId PostedExchangeSetQueueMessageFailed = new(PostedToExchangeSetQueueFailedId, nameof(PostedExchangeSetQueueMessageFailed));

        [LoggerMessage(PostedToExchangeSetQueueFailedId, LogLevel.Error, "Posted to EFS request queue failed: {@message}", EventName = nameof(PostedExchangeSetQueueMessageFailed))]
        public static partial void LogPostedExchangeSetQueueFailedMessage(this ILogger logger, [LogProperties] ExchangeSetRequestMessage message, Exception exception);


        // A status post to the Orchestrator from a builder container has failed
        public static readonly EventId PostedStatusUpdateFromBuilderFailed = new(PostedStatusUpdateFromBuilderFailedId, nameof(PostedStatusUpdateFromBuilderFailed));

        [LoggerMessage(PostedStatusUpdateFromBuilderFailedId, LogLevel.Error, "Posted status update from builder failed: {@message}", EventName = nameof(PostedStatusUpdateFromBuilderFailed))]
        public static partial void LogPostedStatusUpdateFromBuilderFailed(this ILogger logger, [LogProperties] BuildNodeStatus message, Exception exception);


        // A request for the job from a builder container has failed
        public static readonly EventId GetJobRequestFailed = new(GetJobRequestFailedId, nameof(GetJobRequestFailed));

        [LoggerMessage(GetJobRequestFailedId, LogLevel.Error, "Get job request failed for job id: {@jobId}", EventName = nameof(GetJobRequestFailed))]
        public static partial void LogGetJobRequestFailed(this ILogger logger, string jobId);


        // A job has been created
        public static readonly EventId JobCreated = new(JobCreatedId, nameof(JobCreated));

        [LoggerMessage(JobCreatedId, LogLevel.Information, "Job created from correlation id [{@correlationId}]: {@job}", EventName = nameof(JobCreated))]
        public static partial void LogJobCreated(this ILogger logger, string correlationId, [LogProperties] ExchangeSetJobLogView job);


        // A job has been updated
        public static readonly EventId JobUpdated = new(JobUpdatedId, nameof(JobUpdated));

        [LoggerMessage(JobUpdatedId, LogLevel.Information, "Job updated: {@job}", EventName = nameof(JobUpdated))]
        public static partial void LogJobUpdated(this ILogger logger, [LogProperties] ExchangeSetJobLogView job);


        // A job has been completed
        public static readonly EventId JobCompleted = new(JobCompletedId, nameof(JobCompleted));

        [LoggerMessage(JobCompletedId, LogLevel.Information, "Job completed: {@job}", EventName = nameof(JobCompleted))]
        public static partial void LogJobCompleted(this ILogger logger, [LogProperties] ExchangeSetJobLogView job);


        // SCS has returned an error
        public static readonly EventId SalesCatalogueError = new(SalesCatalogueErrorId, nameof(SalesCatalogueError));

        [LoggerMessage(SalesCatalogueErrorId, LogLevel.Error, "Sales Catalogue error: {@error} {@message}", EventName = nameof(SalesCatalogueError))]
        public static partial void LogSalesCatalogueApiError(this ILogger logger, [LogProperties] IError error, [LogProperties] SalesCatalogApiErrorLogView message);

        // SCS has returned an Unexpected status Code
        public static readonly EventId SalesCatalogueUnexpectedStatusCode = new(SalesCatalogueUnexpectedStatusCodeId, nameof(SalesCatalogueUnexpectedStatusCode));

        [LoggerMessage(SalesCatalogueUnexpectedStatusCodeId, LogLevel.Error, "Sales Catalogue Unexpected Status Code: {@salesCatalogueLog}", EventName = nameof(SalesCatalogueUnexpectedStatusCode))]
        public static partial void LogUnexpectedSalesCatalogueStatusCode(this ILogger logger, SalesCatalogUnexpectedStatusLogView salesCatalogueLog);


        // The container execution failed
        public static readonly EventId ContainerExecutionFailed = new(ContainerExecutionFailedId, nameof(ContainerExecutionFailed));

        [LoggerMessage(ContainerExecutionFailedId, LogLevel.Error, "Builder container failed: {@job}", EventName = nameof(ContainerExecutionFailed))]
        public static partial void LogContainerExecutionFailed(this ILogger logger, [LogProperties] ExchangeSetJobLogView job, Exception exception);


        // The container start failed
        public static readonly EventId ContainerStartFailed = new(ContainerStartFailedId, nameof(ContainerStartFailed));

        [LoggerMessage(ContainerStartFailedId, LogLevel.Error, "Builder container start failed: {@containerId}", EventName = nameof(ContainerStartFailed))]
        public static partial void LogContainerStartFailed(this ILogger logger, string containerId);


        // The container wait failed
        public static readonly EventId ContainerWaitFailed = new(ContainerWaitFailedId, nameof(ContainerWaitFailed));

        [LoggerMessage(ContainerWaitFailedId, LogLevel.Error, "Builder container start failed: {@containerId} {@message}", EventName = nameof(ContainerWaitFailed))]
        public static partial void LogContainerWaitFailed(this ILogger logger, string containerId, string message);


        // The container was removed
        public static readonly EventId ContainerRemoved = new(ContainerRemovedId, nameof(ContainerRemoved));

        [LoggerMessage(ContainerRemovedId, LogLevel.Information, "Builder container removed: {@containerId}", EventName = nameof(ContainerRemoved))]
        public static partial void LogContainerRemoved(this ILogger logger, string containerId);


        // The job creation failed
        public static readonly EventId JobCreationFailed = new(JobCreationFailedId, nameof(JobCreationFailed));

        [LoggerMessage(JobCreationFailedId, LogLevel.Error, "Job creation failed", EventName = nameof(JobCreationFailed))]
        public static partial void LogJobCreationFailed(this ILogger logger, Exception exception);


        // The builder container timed out
        public static readonly EventId ContainerTimeout = new(ContainerTimeoutId, nameof(ContainerTimeout));

        [LoggerMessage(ContainerTimeoutId, LogLevel.Error, "Builder container timed out: {@containerId} {@job}", EventName = nameof(ContainerTimeout))]
        public static partial void LogContainerTimeout(this ILogger logger, string containerId, [LogProperties] ExchangeSetJobLogView job);


        // The Queue service failed to read a message
        public static readonly EventId QueueServiceMessageReadFailed = new(ContainerTimeoutId, nameof(QueueServiceMessageReadFailed));

        [LoggerMessage(QueueServiceMessageReadFailedId, LogLevel.Error, "Queue service failed to read message", EventName = nameof(QueueServiceMessageReadFailed))]
        public static partial void LogQueueServiceMessageReadFailed(this ILogger logger, Exception exception);


        // The log forwarder failed to parse a message
        public static readonly EventId LogForwardParseFailed = new(LogForwardParseFailedId, nameof(LogForwardParseFailed));

        [LoggerMessage(LogForwardParseFailedId, LogLevel.Error, "Log forwarding parse failure: {@line}", EventName = nameof(LogForwardParseFailed))]
        public static partial void LogForwarderParseFailure(this ILogger logger, string line, Exception exception);


        // The log forwarder failed to parse a message (null)
        public static readonly EventId LogForwardParseNull = new(LogForwardParseNullId, nameof(LogForwardParseNull));

        [LoggerMessage(LogForwardParseNullId, LogLevel.Error, "Log forwarding parse failure: {@line}", EventName = nameof(LogForwardParseNull))]
        public static partial void LogForwarderParseNull(this ILogger logger, string line);

        // File Share Service operation failed
        public static readonly EventId FileShareServiceOperationFailed = new(FileShareServiceOperationFailedId, nameof(FileShareServiceOperationFailed));

        [LoggerMessage(FileShareServiceOperationFailedId, LogLevel.Error, "File Share Service operation failed: {@fileShareServiceLog}", EventName = nameof(FileShareServiceOperationFailed))]
        public static partial void LogFileShareError(this ILogger logger, [LogProperties] FileShareServiceLogView fileShareServiceLog);

        // Search committed batches failed
        public static readonly EventId SearchCommittedBatchesFailed = new(SearchCommittedBatchesFailedId, nameof(SearchCommittedBatchesFailed));

        [LoggerMessage(SearchCommittedBatchesFailedId, LogLevel.Error, "Search committed batches failed: {@searchCommittedBatchesLogView}", EventName = nameof(SearchCommittedBatchesFailed))]
        public static partial void LogFileShareSearchCommittedBatchesError(this ILogger logger, [LogProperties] SearchCommittedBatchesLog searchCommittedBatchesLogView);
    }
}


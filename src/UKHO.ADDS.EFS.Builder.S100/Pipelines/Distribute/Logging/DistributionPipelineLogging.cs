using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class DistributionPipelineLogging
    {
        private const int BaseEventId = 4000;
        private const int DistributionPipelineFailedId = BaseEventId + 1;
        private const int AddFileNodeFailedId = BaseEventId + 2;
        private const int FileShareAddFileToBatchErrorId = BaseEventId + 3;
        private const int ExtractExchangeSetNodeFailedId = BaseEventId + 4;
        private const int UploadFilesNotFoundId = BaseEventId + 5;
        private const int IICExtractExchangeSetErrorId = BaseEventId + 6;

        // The distribution pipeline failed
        public static readonly EventId DistributionPipelineFailed = new(DistributionPipelineFailedId, nameof(DistributionPipelineFailed));

        [LoggerMessage(DistributionPipelineFailedId, LogLevel.Error, "Distribution pipeline failed: {@result}", EventName = nameof(DistributionPipelineFailed))]
        public static partial void LogDistributionPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        // The Add File Node Failed
        public static readonly EventId AddFileNodeFailed = new(AddFileNodeFailedId, nameof(AddFileNodeFailed));

        // The Add File Node FSS Add File failed
        public static readonly EventId FileShareAddFileToBatchError = new(FileShareAddFileToBatchErrorId, nameof(FileShareAddFileToBatchError));

        [LoggerMessage(AddFileNodeFailedId, LogLevel.Error, "AddFileNode failed: {@errorMessage}", EventName = nameof(AddFileNodeFailed))]
        public static partial void LogUploadFilesNodeFailed(this ILogger logger, string errorMessage);

        [LoggerMessage(FileShareAddFileToBatchErrorId, LogLevel.Error, "AddFileNode File Share Service AddFileToBatch failed: {@addFileLog}", EventName = nameof(FileShareAddFileToBatchError))]
        public static partial void LogFileShareAddFileToBatchError(this ILogger logger, AddFileLogView addFileLog);

        // The Extract ExchangeSet Node failed
        public static readonly EventId ExtractExchangeSetNodeFailed = new(ExtractExchangeSetNodeFailedId, nameof(ExtractExchangeSetNodeFailed));

        [LoggerMessage(ExtractExchangeSetNodeFailedId, LogLevel.Error, "ExtractExchangeSetNode failed: {@errorMessage}", EventName = nameof(ExtractExchangeSetNodeFailed))]
        public static partial void LogExtractExchangeSetNodeFailed(this ILogger logger, string errorMessage);

        // The Upload Files Not Available
        public static readonly EventId UploadFilesNotFound = new(UploadFilesNotFoundId, nameof(UploadFilesNotFound));

        [LoggerMessage(UploadFilesNotFoundId, LogLevel.Error, "UploadFilesNotFound failed: File not found at given path. | {@fileNotFoundLogView}", EventName = nameof(UploadFilesNotFound))]
        public static partial void LogUploadFilesNotFound(this ILogger logger, FileNotFoundLogView fileNotFoundLogView);
        
        // The IIC Extract ExchangeSet Failed Log
        public static readonly EventId IICExtractExchangeSetError = new (IICExtractExchangeSetErrorId, nameof(IICExtractExchangeSetError));

        [LoggerMessage(IICExtractExchangeSetErrorId, LogLevel.Error, "ExtractExchangeSetNode IIC ExtractExchangeSet failed: {@error}", EventName = nameof(IICExtractExchangeSetError))]
        public static partial void LogIICExtractExchangeSetError(this ILogger logger,IError error);

    }
}

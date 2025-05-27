using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Distribute.Logging
{
    internal static partial class DistributionPipelineLogging
    {
        private const int BaseEventId = 4000;
        private const int DistributionPipelineFailedId = BaseEventId + 1;
        private const int AddFileNodeFailedId = BaseEventId + 2;
        private const int AddFileNodeFssAddFileFailedId = BaseEventId + 3;

        // The distribution pipeline failed
        public static readonly EventId DistributionPipelineFailed = new(DistributionPipelineFailedId, nameof(DistributionPipelineFailed));

        [LoggerMessage(DistributionPipelineFailedId, LogLevel.Error, "Distribution pipeline failed: {@result}", EventName = nameof(DistributionPipelineFailed))]
        public static partial void LogDistributionPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);
        
        // The Add File Node Failed
        public static readonly EventId AddFileNodeFailed = new(AddFileNodeFailedId, nameof(AddFileNodeFailed));

        // The Add File Node FSS Add File failed
        public static readonly EventId AddFileNodeFssAddFileFailed = new(AddFileNodeFssAddFileFailedId, nameof(AddFileNodeFssAddFileFailed));

        [LoggerMessage(AddFileNodeFailedId, LogLevel.Error, "AddFileNode failed: {@errorMessage}", EventName = nameof(AddFileNodeFailed))]
        public static partial void LogAddFileNodeFailed(this ILogger logger, string errorMessage);

        [LoggerMessage(AddFileNodeFssAddFileFailedId, LogLevel.Error, "AddFileNode File Share Service AddFile failed: {@addFileLog}", EventName = nameof(AddFileNodeFssAddFileFailed))]
        public static partial void LogAddFileNodeFssAddFileFailed(this ILogger logger, AddFileLogView addFileLog);
    }
}

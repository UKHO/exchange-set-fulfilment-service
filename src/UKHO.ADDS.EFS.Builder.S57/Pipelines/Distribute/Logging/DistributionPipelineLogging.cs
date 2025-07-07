using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Distribute.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class DistributionPipelineLogging
    {
        private const int BaseEventId = 4000;
        private const int DistributionPipelineFailedId = BaseEventId + 1;

        // The distribution pipeline failed
        public static readonly EventId DistributionPipelineFailed = new(DistributionPipelineFailedId, nameof(DistributionPipelineFailed));

        [LoggerMessage(DistributionPipelineFailedId, LogLevel.Error, "Distribution pipeline failed: {@result}", EventName = nameof(DistributionPipelineFailed))]
        public static partial void LogDistributionPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);
    }
}

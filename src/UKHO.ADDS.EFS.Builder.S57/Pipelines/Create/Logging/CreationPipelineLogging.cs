using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Create.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class CreationPipelineLogging
    {
        private const int BaseEventId = 3000;

        private const int CreationPipelineFailedId = BaseEventId + 1;

        // The creation pipeline failed
        public static readonly EventId CreationPipelineFailed = new(CreationPipelineFailedId, nameof(CreationPipelineFailed));

        [LoggerMessage(CreationPipelineFailedId, LogLevel.Error, "Creation pipeline failed: {@result}", EventName = nameof(CreationPipelineFailed))]
        public static partial void LogCreationPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);
    }
}

using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Assemble.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);
    }
}

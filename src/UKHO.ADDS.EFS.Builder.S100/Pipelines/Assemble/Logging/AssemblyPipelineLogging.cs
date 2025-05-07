using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;

        private const int CreateBatchNodeFailedId = BaseEventId + 2;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        [LoggerMessage(CreateBatchNodeFailedId, LogLevel.Error, "CreateBatchNode failed for job id: {@jobId} {@error}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogCreateBatchNodeFailed(this ILogger logger, string jobId, [LogProperties] IError error);
    }
}

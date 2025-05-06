using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;
        private const int ProductSearchPipelineCompletedId = BaseEventId + 2;
        private const int ProductSearchPipelineFailedId = BaseEventId + 3;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));
        public static readonly EventId ProductSearchPipelineCompleted = new(ProductSearchPipelineCompletedId, nameof(ProductSearchPipelineCompleted));
        public static readonly EventId ProductSearchPipelineFailed = new(ProductSearchPipelineFailedId, nameof(ProductSearchPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        [LoggerMessage(ProductSearchPipelineCompletedId, LogLevel.Information, "Product search pipeline Completed, Total Batch Count: {@result}", EventName = nameof(ProductSearchPipelineCompleted))]
        public static partial void LogProductSearchPipelineCompleted(this ILogger logger, [LogProperties] int result);

        [LoggerMessage(ProductSearchPipelineFailedId, LogLevel.Information, "Product search pipeline failed: {@result}", EventName = nameof(ProductSearchPipelineFailed))]
        public static partial void LogProductSearchPipelineFailed(this ILogger logger, [LogProperties] string result);

    }
}

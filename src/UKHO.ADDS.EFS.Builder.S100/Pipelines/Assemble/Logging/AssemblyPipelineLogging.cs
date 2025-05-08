using UKHO.ADDS.Infrastructure.Pipelines.Nodes;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Assemble.Logging
{
    internal static partial class AssemblyPipelineLogging
    {
        private const int BaseEventId = 2000;

        private const int AssemblyPipelineFailedId = BaseEventId + 1;
        private const int CreateBatchNodeFailedId = BaseEventId + 2;
        private const int ProductSearchNodeFailedId = BaseEventId + 3;
        private const int ProductSearchNodeFssSearchFailedId = BaseEventId + 4;

        // The assembly pipeline failed
        public static readonly EventId AssemblyPipelineFailed = new(AssemblyPipelineFailedId, nameof(AssemblyPipelineFailed));

        [LoggerMessage(AssemblyPipelineFailedId, LogLevel.Error, "Assembly pipeline failed: {@result}", EventName = nameof(AssemblyPipelineFailed))]
        public static partial void LogAssemblyPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        // The Create Batch Node Failed
        public static readonly EventId CreateBatchNodeFailed = new(CreateBatchNodeFailedId, nameof(CreateBatchNodeFailed));

        [LoggerMessage(CreateBatchNodeFailedId, LogLevel.Error, "CreateBatchNode failed for job id: {@jobId} {@error}", EventName = nameof(CreateBatchNodeFailed))]
        public static partial void LogCreateBatchNodeFailed(this ILogger logger, string jobId, [LogProperties] IError error); 

        //The Product Search node failed
        public static readonly EventId ProductSearchNodeFailed = new(ProductSearchNodeFailedId, nameof(ProductSearchNodeFailed));

        [LoggerMessage(ProductSearchNodeFailedId, LogLevel.Error, "ProductSearchNode failed: {@errorMessage}", EventName = nameof(ProductSearchNodeFailed))]
        public static partial void LogProductSearchNodeFailed(this ILogger logger, string errorMessage);

        //The Product Search node FSS Search failed
        public static readonly EventId ProductSearchNodeFssSearchFailed = new(ProductSearchNodeFssSearchFailedId, nameof(ProductSearchNodeFssSearchFailed));

        [LoggerMessage(ProductSearchNodeFssSearchFailedId, LogLevel.Error, "ProductSearchNode File Share Service Search failed: {@error}", EventName = nameof(ProductSearchNodeFssSearchFailed))]
        public static partial void LogProductSearchNodeFssSearchFailed(this ILogger logger, [LogProperties] IError error);
    }
}

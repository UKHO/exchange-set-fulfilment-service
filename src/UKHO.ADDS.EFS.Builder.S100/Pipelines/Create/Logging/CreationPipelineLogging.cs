using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Create.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class CreationPipelineLogging
    {
        private const int BaseEventId = 3000;

        private const int CreationPipelineFailedId = BaseEventId + 1;
        private const int AddContentExchangeSetNodeFailedId = BaseEventId + 2;
        private const int SignExchangeSetNodeFailedId = BaseEventId + 3;
        private const int AddExchangeSetNodeFailedId = BaseEventId + 4;

        // The creation pipeline failed
        public static readonly EventId CreationPipelineFailed = new(CreationPipelineFailedId, nameof(CreationPipelineFailed));

        [LoggerMessage(CreationPipelineFailedId, LogLevel.Error, "Creation pipeline failed: {@result}", EventName = nameof(CreationPipelineFailed))]
        public static partial void LogCreationPipelineFailed(this ILogger logger, [LogProperties] NodeResultLogView result);

        // The Add Content ExchangeSet Node Failed
        public static readonly EventId AddContentExchangeSetNodeFailed = new(AddContentExchangeSetNodeFailedId, nameof(AddContentExchangeSetNodeFailed));

        [LoggerMessage(AddContentExchangeSetNodeFailedId, LogLevel.Error, "AddContentExchangeSetNode failed: {@error}", EventName = nameof(AddContentExchangeSetNodeFailed))]
        public static partial void LogAddContentExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        // The Sign Exchange Set Node Failed
        public static readonly EventId SignExchangeSetNodeFailed = new(SignExchangeSetNodeFailedId, nameof(SignExchangeSetNodeFailed));

        [LoggerMessage(SignExchangeSetNodeFailedId, LogLevel.Error, "SignExchangeSetNode failed: {@error}", EventName = nameof(SignExchangeSetNodeFailed))]
        public static partial void LogSignExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

        // The Create Exchange Set Node Failed
        public static readonly EventId AddExchangeSetNodeFailed = new(AddExchangeSetNodeFailedId, nameof(AddExchangeSetNodeFailed));

        [LoggerMessage(AddExchangeSetNodeFailedId, LogLevel.Error, "AddExchangeSetNode failed: {@error}", EventName = nameof(AddExchangeSetNodeFailed))]
        public static partial void LogAddExchangeSetNodeFailed(this ILogger logger, [LogProperties] IError error);

    }
}

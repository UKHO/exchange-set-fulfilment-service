using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class StartupPipelineLogging
    {
        private const int BaseEventId = 1000;

        private const int StartupPipelineFailedId = BaseEventId + 1;
        private const int JobRetrievedId = BaseEventId + 2;
        private const int DebugJobWarningId = BaseEventId + 3;
        private const int StartupConfigurationId = BaseEventId + 4;

        // The startup pipeline failed
        public static readonly EventId StartupPipelineFailed = new(StartupPipelineFailedId, nameof(StartupPipelineFailed));


        // The job was received
        public static readonly EventId JobRetrieved = new(JobRetrievedId, nameof(JobRetrieved));


        // Debug job warning
        public static readonly EventId DebugJobWarning = new(DebugJobWarningId, nameof(DebugJobWarning));


        // startup configuration
        public static readonly EventId StartupConfiguration = new(StartupConfigurationId, nameof(StartupConfiguration));

        [LoggerMessage(StartupPipelineFailedId, LogLevel.Error, "Startup pipeline failed: {@result}", EventName = nameof(StartupPipelineFailed))]
        public static partial void LogStartupPipelineFailed(this ILogger logger, [LogProperties] NodeResult result);

        [LoggerMessage(JobRetrievedId, LogLevel.Information, "Job retrieved: {@job}", EventName = nameof(JobRetrieved))]
        public static partial void LogJobRetrieved(this ILogger logger, [LogProperties] ExchangeSetJobLogView job);

        [LoggerMessage(DebugJobWarningId, LogLevel.Warning, "Debug job", EventName = nameof(DebugJobWarning))]
        public static partial void LogDebugJobWarning(this ILogger logger);

        [LoggerMessage(StartupConfigurationId, LogLevel.Information, "Startup configuration: {@configuration}", EventName = nameof(StartupConfiguration))]
        public static partial void LogStartupConfiguration(this ILogger logger, [LogProperties] ConfigurationLogView configuration);
    }
}

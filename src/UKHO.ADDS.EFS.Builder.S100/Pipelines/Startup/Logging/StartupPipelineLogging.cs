using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class StartupPipelineLogging
    {
        private const int BaseEventId = 1000;

        private const int StartupPipelineFailedId = BaseEventId + 1;
        private const int JobRetrievedId = BaseEventId + 2;
        private const int DebugJobWarningId = BaseEventId + 3;
        private const int StartupConfigurationId = BaseEventId + 4;
        private const int TomcatMessageId = BaseEventId + 5;

        // The startup pipeline failed
        public static readonly EventId StartupPipelineFailed = new(StartupPipelineFailedId, nameof(StartupPipelineFailed));

        [LoggerMessage(StartupPipelineFailedId, LogLevel.Error, "Startup pipeline failed: {@result}", EventName = nameof(StartupPipelineFailed))]
        public static partial void LogStartupPipelineFailed(this ILogger logger, [LogProperties] NodeResultLogView result);


        // The build was retrieved
        public static readonly EventId JobRetrieved = new(JobRetrievedId, nameof(JobRetrieved));

        [LoggerMessage(JobRetrievedId, LogLevel.Information, "Job retrieved: {@job}", EventName = nameof(JobRetrieved))]
        public static partial void LogBuildRetrieved(this ILogger logger, [LogProperties] S100BuildLogView job);


        // startup configuration
        public static readonly EventId StartupConfiguration = new(StartupConfigurationId, nameof(StartupConfiguration));

        [LoggerMessage(StartupConfigurationId, LogLevel.Information, "Startup configuration: {@configuration}", EventName = nameof(StartupConfiguration))]
        public static partial void LogStartupConfiguration(this ILogger logger, [LogProperties] ConfigurationLogView configuration);


        // Tomcat message
        public static readonly EventId TomcatMessage = new(TomcatMessageId, nameof(TomcatMessage));

        [LoggerMessage(TomcatMessageId, LogLevel.Debug, "Tomcat: {@message}", EventName = nameof(TomcatMessage))]
        public static partial void LogTomcatMessage(this ILogger logger, [LogProperties] TomcatLogView message);
    }
}

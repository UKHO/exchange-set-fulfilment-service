namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation.AzureStorageEventLogging.Enums
{
    /// <summary>
    ///     The Azure Storage Logging Check Result enumeration
    /// </summary>
    public enum AzureStorageLoggingCheckResult
    {
        NoLogging = 0,
        LogWarningNoStorage = 1,
        LogWarningAndStoreMessage = 2
    }
}

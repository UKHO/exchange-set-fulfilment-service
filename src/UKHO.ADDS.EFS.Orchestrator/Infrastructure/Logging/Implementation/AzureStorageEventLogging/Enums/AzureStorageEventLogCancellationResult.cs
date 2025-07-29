using System.ComponentModel;

namespace UKHO.Logging.EventHubLogProvider.AzureStorageEventLogging.Enums
{
    /// <summary>
    ///     The Cancellation Result enumeration
    /// </summary>
    public enum AzureStorageEventLogCancellationResult
    {
        [Description("Indicates successful cancellation")]
        Successful = 0,
        [Description("Indicates unsuccessful cancellation due to process been completed or at the late stage")]
        UnableToCancel = 1,
        [Description("Indicates un-successful cancellation due to thrown exception")]
        CancellationFailed = 2
    }
}
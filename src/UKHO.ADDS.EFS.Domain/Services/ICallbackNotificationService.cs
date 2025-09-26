using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services
{
    /// <summary>
    /// Service for sending callback notifications when Exchange Sets are committed
    /// </summary>
    public interface ICallbackNotificationService
    {
        /// <summary>
        /// Sends a callback notification to the specified URI when an Exchange Set is committed
        /// </summary>
        /// <param name="job">The job containing callback URI and exchange set details</param>
        /// <param name="exchangeSetData">The exchange set data to include in the notification</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SendCallbackNotificationAsync(Job job, object exchangeSetData, CancellationToken cancellationToken);
    }
}

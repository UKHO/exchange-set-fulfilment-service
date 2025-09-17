using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Domain.Services
{
    /// <summary>
    /// Service for making callback notifications
    /// </summary>
    public interface ICallbackService
    {
        /// <summary>
        /// Sends a callback notification to the specified URI with the exchange set response data
        /// </summary>
        /// <param name="callbackUri">The URI to send the callback to</param>
        /// <param name="responseData">The exchange set response data</param>
        /// <param name="correlationId">The correlation ID for logging</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the callback was sent successfully, false otherwise</returns>
        Task<bool> SendCallbackAsync(CallbackUri callbackUri, object responseData, CorrelationId correlationId, CancellationToken cancellationToken = default);
    }
}

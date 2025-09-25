using System.Text;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Domain.Services.Models;
using UKHO.ADDS.EFS.Infrastructure.Logging;
using UKHO.ADDS.EFS.Infrastructure.Logging.Services;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for sending callback notifications when Exchange Sets are committed
    /// </summary>
    internal class CallbackNotificationService : ICallbackNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CallbackNotificationService> _logger;

        public CallbackNotificationService(IHttpClientFactory httpClientFactory, ILogger<CallbackNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a callback notification to the specified URI when an Exchange Set is committed
        /// </summary>
        /// <param name="job">The job containing callback URI and exchange set details</param>
        /// <param name="exchangeSetData">The exchange set data to include in the notification</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task SendCallbackNotificationAsync(Job job, object exchangeSetData, CancellationToken cancellationToken)
        {
            if (job.CallbackUri == CallbackUri.None)
            {
                LogCallbackNotificationSkipped(job.Id, job.CallbackUri, job.GetCorrelationId(), job.BatchId);
                return;
            }

            try
            {
                var cloudEvent = new CloudEventNotification
                {
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"),
                    Data = exchangeSetData
                };

                var json = JsonCodec.Encode(cloudEvent);
                var content = new StringContent(json, Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

                using var httpClient = _httpClientFactory.CreateClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, job.CallbackUri.Value)
                {
                    Content = content
                };
                
                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    LogCallbackNotificationSuccess(job.Id, job.CallbackUri, job.GetCorrelationId(), job.BatchId, (int)response.StatusCode, responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    LogCallbackNotificationFailure(job.Id, job.CallbackUri, job.GetCorrelationId(), job.BatchId, (int)response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                LogCallbackNotificationError(job.Id, job.CallbackUri, job.GetCorrelationId(), job.BatchId, ex);
                // Note: We don't rethrow the exception as the callback failure shouldn't fail the entire job completion
            }
        }

        /// <summary>
        /// Logs when no callback URI is provided for a job
        /// </summary>
        /// <param name="jobId">The job identifier</param>
        /// <param name="callbackUri">The callback URI</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="batchId">The batch identifier</param>
        private void LogCallbackNotificationSkipped(JobId jobId, CallbackUri callbackUri, CorrelationId correlationId, BatchId batchId)
        {
            var logView = new CallbackNotificationLogView
            {
                JobId = jobId,
                CallbackUri = callbackUri,
                CorrelationId = correlationId,
                BatchId = batchId
            };

            _logger.LogCallbackNotificationSkipped(logView);
        }

        /// <summary>
        /// Logs when a callback notification is sent successfully
        /// </summary>
        /// <param name="jobId">The job identifier</param>
        /// <param name="callbackUri">The callback URI</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="batchId">The batch identifier</param>
        /// <param name="statusCode">The HTTP response status code</param>
        /// <param name="responseContent">The response content from the callback endpoint</param>
        private void LogCallbackNotificationSuccess(JobId jobId, CallbackUri callbackUri, CorrelationId correlationId, BatchId batchId, int statusCode, string responseContent)
        {
            var logView = new CallbackNotificationLogView
            {
                JobId = jobId,
                CallbackUri = callbackUri,
                CorrelationId = correlationId,
                BatchId = batchId,
                StatusCode = statusCode,
                ResponseContent = responseContent
            };

            _logger.LogCallbackNotificationSuccess(logView);
        }

        /// <summary>
        /// Logs when a callback notification fails with an HTTP error response
        /// </summary>
        /// <param name="jobId">The job identifier</param>
        /// <param name="callbackUri">The callback URI</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="batchId">The batch identifier</param>
        /// <param name="statusCode">The HTTP response status code</param>
        /// <param name="errorContent">The error response content</param>
        private void LogCallbackNotificationFailure(JobId jobId, CallbackUri callbackUri, CorrelationId correlationId, BatchId batchId, int statusCode, string errorContent)
        {
            var logView = new CallbackNotificationLogView
            {
                JobId = jobId,
                CallbackUri = callbackUri,
                CorrelationId = correlationId,
                BatchId = batchId,
                StatusCode = statusCode,
                ErrorContent = errorContent
            };

            _logger.LogCallbackNotificationFailed(logView);
        }

        /// <summary>
        /// Logs when a callback notification encounters an exception
        /// </summary>
        /// <param name="jobId">The job identifier</param>
        /// <param name="callbackUri">The callback URI</param>
        /// <param name="correlationId">The correlation identifier</param>
        /// <param name="batchId">The batch identifier</param>
        /// <param name="exception">The exception that occurred</param>
        private void LogCallbackNotificationError(JobId jobId, CallbackUri callbackUri, CorrelationId correlationId, BatchId batchId, Exception exception)
        {
            var logView = new CallbackNotificationLogView
            {
                JobId = jobId,
                CallbackUri = callbackUri,
                CorrelationId = correlationId,
                BatchId = batchId
            };

            _logger.LogCallbackNotificationError(logView, exception);
        }
    }
}

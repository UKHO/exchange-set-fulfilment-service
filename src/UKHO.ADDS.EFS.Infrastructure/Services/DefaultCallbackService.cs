using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Domain.External;
using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services;
using UKHO.ADDS.EFS.Infrastructure.Models.CloudEvents;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Infrastructure.Services
{
    /// <summary>
    /// Default implementation of the callback service
    /// </summary>
    public class DefaultCallbackService : ICallbackService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DefaultCallbackService> _logger;

        public DefaultCallbackService(HttpClient httpClient, ILogger<DefaultCallbackService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendCallbackAsync(CallbackUri callbackUri, object responseData, CorrelationId correlationId, CancellationToken cancellationToken = default)
        {
            if (callbackUri == CallbackUri.None)
            {
                _logger.LogInformation("No callback URI provided for correlation ID {CorrelationId}, skipping callback notification", correlationId);
                return true;
            }

            try
            {
                var cloudEvent = new CloudEventMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow,
                    Data = responseData
                };
                
                var content = new StringContent(JsonCodec.Encode(cloudEvent), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);

                _logger.LogInformation("Sending callback notification to {CallbackUri} for correlation ID {CorrelationId}", callbackUri.Value, correlationId);

                var response = await _httpClient.PostAsync(callbackUri.Value, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("Callback notification sent successfully to {CallbackUri} for correlation ID {CorrelationId}. Response: {StatusCode} {ResponseBody}", 
                        callbackUri.Value, correlationId, response.StatusCode, responseBody);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Callback notification failed for {CallbackUri} with correlation ID {CorrelationId}. Response: {StatusCode} {ErrorBody}", 
                        callbackUri.Value, correlationId, response.StatusCode, errorBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending callback notification to {CallbackUri} for correlation ID {CorrelationId}", 
                    callbackUri.Value, correlationId);
                return false;
            }
        }
    }
}

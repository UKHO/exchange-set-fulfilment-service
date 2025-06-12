using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using UKHO.ADDS.Infrastructure.Results; // Added for IError

namespace UKHO.ADDS.EFS.Domain.RetryPolicy
{
    /// <summary>
    /// Provides Polly retry policies for HttpClient and custom result types to handle transient errors.
    /// </summary>
    public static class HttpClientPolicyProvider
    {
        private static readonly int[] RetriableStatusCodes =
        [
            408, // Request Timeout
            409, // Conflict
            429, // Too Many Requests
            502, // Bad Gateway
            503, // Service Unavailable
            504  // Gateway Timeout
        ];

        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 10000;

        // Static configuration instance
        private static IConfiguration? _configuration;
        public static void SetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Helper to read retry settings from configuration or use defaults
        private static (int maxRetryAttempts, int retryDelayMs) GetRetrySettings()
        {
            int maxRetryAttempts = MaxRetryAttempts;
            int retryDelayMs = RetryDelayMs;
            if (_configuration != null)
            {
                int.TryParse(_configuration["HttpRetry:MaxRetryAttempts"], out maxRetryAttempts);
                int.TryParse(_configuration["HttpRetry:RetryDelayMs"], out retryDelayMs);
                if (maxRetryAttempts <= 0) maxRetryAttempts = MaxRetryAttempts;
                if (retryDelayMs <= 0) retryDelayMs = RetryDelayMs;
            }
            return (maxRetryAttempts, retryDelayMs);
        }

        // Private helper for logging retry attempts (structured)
        private static void LogRetryAttempt(
            ILogger logger,
            DateTimeOffset timestamp,
            int retryAttempt,
            int maxRetryAttempts,
            string urlOrType,
            string statusCode,
            double delaySeconds)
        {
            logger.LogHttpRetryAttempt(
                timestamp,
                retryAttempt,
                maxRetryAttempts,
                urlOrType,
                statusCode,
                delaySeconds
            );
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            var (maxRetryAttempts, retryDelayMs) = GetRetrySettings();

            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => RetriableStatusCodes.Contains((int)r.StatusCode))
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    _ => TimeSpan.FromMilliseconds(retryDelayMs),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        var statusCode = outcome.Exception != null ? "Exception" : ((int)outcome.Result.StatusCode).ToString();
                        var url = outcome.Exception != null ? "N/A" : outcome.Result.RequestMessage?.RequestUri?.ToString() ?? "N/A";
                        LogRetryAttempt(
                            logger,
                            DateTimeOffset.UtcNow,
                            retryAttempt,
                            maxRetryAttempts,
                            url,
                            statusCode,
                            timespan.TotalSeconds
                        );
                    }
                );
        }

        // Generic retry policy for custom result types (e.g., IResult<T>)
        public static IAsyncPolicy<T> GetRetryPolicy<T>(ILogger logger, Func<T, int?> getStatusCode)
        {
            var (maxRetryAttempts, retryDelayMs) = GetRetrySettings();

            return Policy<T>
                .HandleResult(r =>
                {
                    var statusCode = getStatusCode(r);
                    return statusCode.HasValue && RetriableStatusCodes.Contains(statusCode.Value);
                })
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    _ => TimeSpan.FromMilliseconds(retryDelayMs),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        var statusCode = getStatusCode(outcome.Result);
                        LogRetryAttempt(
                            logger,
                            DateTimeOffset.UtcNow,
                            retryAttempt,
                            maxRetryAttempts,
                            typeof(T).Name,
                            statusCode?.ToString() ?? "N/A",
                            timespan.TotalSeconds
                        );
                    }
                );
        }

        // Provides a generic retry policy for IResult<T> where error extraction and status code logic is standardized
        public static IAsyncPolicy<IResult<T>> GetGenericResultRetryPolicy<T>(ILogger logger)
        {
            return GetRetryPolicy<IResult<T>>(
                logger,
                r =>
                {
                    r.IsFailure(out var error, out var _);
                    return GetStatusCodeFromError(error);
                }
            );
        }

        // Extracted from CreateBatchNode: Gets status code from IError metadata
        public static int? GetStatusCodeFromError(IError error)
        {
            if (error != null && error.Metadata != null && error.Metadata.ContainsKey("StatusCode"))
                return Convert.ToInt32(error.Metadata["StatusCode"]);
            return null;
        }
    }
}

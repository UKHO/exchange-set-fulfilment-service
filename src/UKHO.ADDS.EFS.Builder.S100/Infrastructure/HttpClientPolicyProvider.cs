using Polly;

namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
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

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, IConfiguration? configuration = null)
        {
            // Read retry settings from configuration if available
            int maxRetryAttempts = MaxRetryAttempts;
            int retryDelayMs = RetryDelayMs;
            if (configuration != null)
            {
                int.TryParse(configuration["HttpRetry:MaxRetryAttempts"], out maxRetryAttempts);
                int.TryParse(configuration["HttpRetry:RetryDelayMs"], out retryDelayMs);
                if (maxRetryAttempts <= 0) maxRetryAttempts = MaxRetryAttempts;
                if (retryDelayMs <= 0) retryDelayMs = RetryDelayMs;
            }

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
                        HttpClientPolicyLogger.LogHttpRetryAttempt(
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
        public static IAsyncPolicy<T> GetRetryPolicy<T>(ILogger logger, Func<T, int?> getStatusCode, IConfiguration? configuration = null)
        {
            int maxRetryAttempts = MaxRetryAttempts;
            int retryDelayMs = RetryDelayMs;
            if (configuration != null)
            {
                int.TryParse(configuration["HttpRetry:MaxRetryAttempts"], out maxRetryAttempts);
                int.TryParse(configuration["HttpRetry:RetryDelayMs"], out retryDelayMs);
                if (maxRetryAttempts <= 0) maxRetryAttempts = MaxRetryAttempts;
                if (retryDelayMs <= 0) retryDelayMs = RetryDelayMs;
            }

            return Policy<T>
                .HandleResult(r =>
                {
                    var statusCode = getStatusCode(r);
                    return statusCode.HasValue && RetriableStatusCodes.Contains(statusCode.Value);
                })
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    _ => TimeSpan.FromMilliseconds(RetryDelayMs),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        var statusCode = getStatusCode(outcome.Result);
                        HttpClientPolicyLogger.LogHttpRetryAttempt(
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
    }
}

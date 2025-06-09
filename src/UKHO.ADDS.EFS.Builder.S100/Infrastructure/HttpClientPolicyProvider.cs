using Polly;

namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
{
    /// <summary>
    /// Provides a Polly retry policy for HttpClient to handle transient errors.
    /// </summary>
    public static class HttpClientPolicyProvider
    {
        private static readonly int[] RetriableStatusCodes =
        [
            408, // Request Timeout
            429, // Too Many Requests
            502, // Bad Gateway
            503, // Service Unavailable
            504  // Gateway Timeout
        ];

        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 10000;

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => RetriableStatusCodes.Contains((int)r.StatusCode))
                .WaitAndRetryAsync(
                    MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(RetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        var statusCode = outcome.Exception != null ? "Exception" : ((int)outcome.Result.StatusCode).ToString();
                        var url = outcome.Exception != null ? "N/A" : outcome.Result.RequestMessage?.RequestUri?.ToString() ?? "N/A";
                        HttpClientPolicyLogger.LogHttpRetryAttempt(
                            logger,
                            DateTimeOffset.UtcNow,
                            retryAttempt,
                            MaxRetryAttempts,
                            url,
                            statusCode,
                            timespan.TotalSeconds
                        );
                    }
                );
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using UKHO.ADDS.Infrastructure.Results; // Added for IError
using System.Collections.Generic;

namespace UKHO.ADDS.EFS.Domain.RetryPolicy
{
    /// <summary>
    /// Provides Polly retry policies for HttpClient and custom result types to handle transient errors.
    /// </summary>
    public static class HttpClientPolicyProvider
    {
        private static readonly HashSet<int> RetriableStatusCodes = new()
        {
            408, // Request Timeout
            429, // Too Many Requests
            502, // Bad Gateway
            503, // Service Unavailable
            504  // Gateway Timeout
        };

        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 10000;

        private static IConfiguration? _configuration;
        public static void SetConfiguration(IConfiguration configuration) => _configuration = configuration;

        private static (int maxRetryAttempts, int retryDelayMs) GetRetrySettings()
        {
            int maxRetryAttempts = MaxRetryAttempts;
            int retryDelayMs = RetryDelayMs;
            if (_configuration != null)
            {
                if (!int.TryParse(_configuration["HttpRetry:MaxRetryAttempts"], out maxRetryAttempts) || maxRetryAttempts <= 0)
                    maxRetryAttempts = MaxRetryAttempts;
                if (!int.TryParse(_configuration["HttpRetry:RetryDelayMs"], out retryDelayMs) || retryDelayMs <= 0)
                    retryDelayMs = RetryDelayMs;
            }
            return (maxRetryAttempts, retryDelayMs);
        }

        private static void LogRetryAttempt(
            ILogger logger,
            DateTimeOffset timestamp,
            int retryAttempt,
            int maxRetryAttempts,
            string urlOrType,
            string statusCode,
            double delaySeconds)
            => logger.LogHttpRetryAttempt(timestamp, retryAttempt, maxRetryAttempts, urlOrType, statusCode, delaySeconds);

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            var (maxRetryAttempts, retryDelayMs) = GetRetrySettings();

            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => RetriableStatusCodes.Contains((int)r.StatusCode))
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt - 1) * retryDelayMs),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        int? statusCodeInt = outcome.Result != null ? (int)outcome.Result.StatusCode : null;
                        string statusCode = statusCodeInt?.ToString() ?? outcome.Exception?.StackTrace ?? "Unknown";
                        string url = outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "N/A";

                        if (statusCodeInt.HasValue && RetriableStatusCodes.Contains(statusCodeInt.Value))
                        {
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
                    }
                );
        }

        public static IAsyncPolicy<T> GetRetryPolicy<T>(ILogger logger, Func<T, int?> getStatusCode, string methodName)
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
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt - 1) * retryDelayMs),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        var statusCode = getStatusCode(outcome.Result);
                        LogRetryAttempt(
                            logger,
                            DateTimeOffset.UtcNow,
                            retryAttempt,
                            maxRetryAttempts,
                            methodName,
                            statusCode?.ToString() ?? "N/A",
                            timespan.TotalSeconds
                        );
                    }
                );
        }

        public static IAsyncPolicy<IResult<T>> GetGenericResultRetryPolicy<T>(ILogger logger, string methodName) =>
            GetRetryPolicy<IResult<T>>(
                logger,
                r =>
                {
                    r.IsFailure(out var error, out var _);
                    return GetStatusCodeFromError(error);
                },
                methodName);

        public static int? GetStatusCodeFromError(IError error) =>
            error?.Metadata != null && error.Metadata.ContainsKey("StatusCode")
                ? Convert.ToInt32(error.Metadata["StatusCode"])
                : null;
    }
}

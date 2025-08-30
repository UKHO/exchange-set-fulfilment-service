using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Orchestrator;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Domain.Services.Infrastructure.Retries
{
    /// <summary>
    /// Provides Polly retry policies for HttpClient and custom result types to handle transient errors.
    /// </summary>
    public static class HttpRetryPolicyFactory
    {
        private static readonly HashSet<int> _retryStatusCodes = new()
        {
            408, // Request Timeout
            429, // Too Many Requests
            502, // Bad Gateway
            503, // Service Unavailable
            504  // Gateway Timeout
        };

        private const int MaxRetryAttempts = 3;
        private const int RetryDelayInMilliseconds = 10000;

        private static IConfiguration? _configuration;

        /// <summary>
        /// Sets the static configuration instance for reading retry settings.
        /// </summary>
        /// <param name="configuration">The configuration instance to use.</param>
        public static void SetConfiguration(IConfiguration configuration) => _configuration = configuration;

        /// <summary>
        /// Gets the retry settings (max attempts and delay) from configuration or defaults.
        /// </summary>
        /// <returns>A tuple containing max retry attempts and retry delay in milliseconds.</returns>
        private static (int maxRetryAttempts, int retryDelayMs) LoadRetrySettings()
        {
            var maxRetryAttempts = MaxRetryAttempts;
            var retryDelayInMilliseconds = RetryDelayInMilliseconds;

            if (_configuration != null)
            {
                if (!int.TryParse(_configuration["HttpRetry:MaxRetryAttempts"], out maxRetryAttempts) || maxRetryAttempts <= 0)
                {
                    maxRetryAttempts = MaxRetryAttempts;
                }

                if (!int.TryParse(_configuration["HttpRetry:RetryDelayInMilliseconds"], out retryDelayInMilliseconds) || retryDelayInMilliseconds <= 0)
                {
                    retryDelayInMilliseconds = RetryDelayInMilliseconds;
                }

                if (!int.TryParse(_configuration[BuilderEnvironmentVariables.MaxRetryAttempts], out maxRetryAttempts) || maxRetryAttempts <= 0)
                {
                    maxRetryAttempts = MaxRetryAttempts;
                }

                if (!int.TryParse(_configuration[BuilderEnvironmentVariables.RetryDelayMilliseconds], out retryDelayInMilliseconds) || retryDelayInMilliseconds <= 0)
                {
                    retryDelayInMilliseconds = RetryDelayInMilliseconds;
                }
            }

            return (maxRetryAttempts, retryDelayInMilliseconds);
        }

        /// <summary>
        /// Logs a retry attempt using the provided logger and retry details.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="timestamp">The timestamp of the retry attempt.</param>
        /// <param name="retryAttempt">The current retry attempt number.</param>
        /// <param name="maxRetryAttempts">The maximum number of retry attempts.</param>
        /// <param name="urlOrType">The URL or method/type being retried.</param>
        /// <param name="statusCode">The status code or error information.</param>
        /// <param name="delaySeconds">The delay before the next retry, in seconds.</param>
        private static void LogRetryAttempt(
                    ILogger logger,
                    DateTimeOffset timestamp,
                    int retryAttempt,
                    int maxRetryAttempts,
                    string urlOrType,
                    string statusCode,
                    double delaySeconds)
                    => logger.LogHttpRetryAttempt(timestamp, retryAttempt, maxRetryAttempts, urlOrType, statusCode, delaySeconds);

        /// <summary>
        /// Gets a Polly async retry policy for HttpClient that retries on transient errors and retriable status codes.
        /// </summary>
        /// <param name="logger">The logger instance for logging retry attempts.</param>
        /// <returns>An IAsyncPolicy for HttpResponseMessage with exponential backoff.</returns>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            var (maxRetryAttempts, retryDelayInMilliseconds) = LoadRetrySettings();

            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => _retryStatusCodes.Contains((int)r.StatusCode))
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt - 1) * retryDelayInMilliseconds),
                    (outcome, timespan, retryAttempt, _) =>
                    {
                        int? statusCodeInt = outcome.Result != null ? (int)outcome.Result.StatusCode : null;
                        var statusCode = statusCodeInt?.ToString() ?? outcome.Exception?.StackTrace ?? "Unknown";
                        var url = outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "N/A";

                        if (statusCodeInt.HasValue && _retryStatusCodes.Contains(statusCodeInt.Value))
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

        /// <summary>
        /// Gets a Polly async retry policy for custom result types, using a status code selector and method name for logging.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="logger">The logger instance for logging retry attempts.</param>
        /// <param name="getStatusCode">A function to extract the status code from the result.</param>
        /// <param name="methodName">The method or operation name for logging.</param>
        /// <returns>An IAsyncPolicy for the specified result type with exponential backoff.</returns>
        public static IAsyncPolicy<T> GetRetryPolicy<T>(ILogger logger, Func<T, int?> getStatusCode, string methodName)
        {
            var (maxRetryAttempts, retryDelayInMilliseconds) = LoadRetrySettings();

            return Policy<T>
                .HandleResult(r =>
                {
                    var statusCode = getStatusCode(r);
                    return statusCode.HasValue && _retryStatusCodes.Contains(statusCode.Value);
                })
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt - 1) * retryDelayInMilliseconds),
                    (outcome, timespan, retryAttempt, _) =>
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

        /// <summary>
        /// Gets a Polly async retry policy for IResult<T> types, extracting status code from error metadata.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="logger">The logger instance for logging retry attempts.</param>
        /// <param name="methodName">The method or operation name for logging.</param>
        /// <returns>An IAsyncPolicy for IResult<T> with exponential backoff.</returns>
        public static IAsyncPolicy<IResult<T>> GetGenericResultRetryPolicy<T>(ILogger logger, string methodName) =>
            GetRetryPolicy<IResult<T>>(
                logger,
                r =>
                {
                    r.IsFailure(out var error, out _);
                    return ExtractStatusCodeFromError(error);
                },
                methodName);

        /// <summary>
        /// Extracts the status code from an IError's metadata, if present.
        /// </summary>
        /// <param name="error">The error object.</param>
        /// <returns>The status code if found; otherwise, null.</returns>
        public static int? ExtractStatusCodeFromError(IError error) =>
            error?.Metadata != null && error.Metadata.ContainsKey("StatusCode")
                ? Convert.ToInt32(error.Metadata["StatusCode"])
                : null;
    }
}

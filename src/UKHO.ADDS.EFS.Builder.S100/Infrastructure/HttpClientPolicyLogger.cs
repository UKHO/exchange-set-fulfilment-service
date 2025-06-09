using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
{
    public static partial class HttpClientPolicyLogger
    {
        [LoggerMessage(
            EventId = 10001,
            Level = LogLevel.Warning,
            Message = "[{Timestamp}] Attempt {RetryAttempt}/{MaxRetryAttempts} for [{Url}] failed with {StatusCode}. Retrying in {DelaySeconds}s...")]
        public static partial void LogHttpRetryAttempt(
            ILogger logger,
            DateTimeOffset timestamp,
            int retryAttempt,
            int maxRetryAttempts,
            string url,
            string statusCode,
            double delaySeconds);
    }
}

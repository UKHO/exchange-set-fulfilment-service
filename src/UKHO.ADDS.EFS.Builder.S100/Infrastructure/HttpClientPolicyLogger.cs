using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Builder.S100.Infrastructure
{
    public static partial class HttpClientPolicyLogger
    {
        private const int HttpRetryAttemptEventId = 10001;
        public static readonly EventId HttpRetryAttempt = new(HttpRetryAttemptEventId, nameof(HttpRetryAttempt));

        [LoggerMessage(
            EventId = HttpRetryAttemptEventId,
            Level = LogLevel.Warning,
            Message = "[{Timestamp}] Attempt {RetryAttempt}/{MaxRetryAttempts} for [{Url}] failed with {StatusCode}. Retrying in {DelaySeconds}s...",
            EventName = nameof(HttpRetryAttempt))]
        public static partial void LogHttpRetryAttempt(
            this ILogger logger,
            DateTimeOffset timestamp,
            int retryAttempt,
            int maxRetryAttempts,
            string url,
            string statusCode,
            double delaySeconds);
    }
}

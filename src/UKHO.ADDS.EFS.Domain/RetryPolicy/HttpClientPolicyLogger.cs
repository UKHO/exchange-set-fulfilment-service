using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.RetryPolicy
{
    public static partial class HttpClientPolicyLogger
    {
        private const int HttpRetryAttemptEventId = 10001;
        public static readonly EventId HttpRetryAttempt = new(HttpRetryAttemptEventId, nameof(HttpRetryAttempt));

        [LoggerMessage(
            EventId = HttpRetryAttemptEventId,
            Level = LogLevel.Warning,
            Message = "[{Timestamp}] Retry request for [{methodName}] with delay {delaySeconds}s and retry attempt {RetryAttempt}/{MaxRetryAttempts} as previous request was responded with {StatusCode}.",
            EventName = nameof(HttpRetryAttempt))]
        public static partial void LogHttpRetryAttempt(
            this ILogger logger,
            DateTimeOffset timestamp,
            int retryAttempt,
            int maxRetryAttempts,
            string methodName,
            string statusCode,
            double delaySeconds);
    }
}

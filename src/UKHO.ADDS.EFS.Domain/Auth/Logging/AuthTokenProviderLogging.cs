using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Auth.Logging
{
    [ExcludeFromCodeCoverage]
    internal static partial class AuthTokenProviderLogging
    {
        private const int BaseEventId = 10000;

        private const int GetAuthTokenFailedId = BaseEventId + 1;
        private const int TokenCacheFailedId = BaseEventId + 2;
        private const int TokenCredentialFailedId = BaseEventId + 3;

        // Auth token acquisition failed
        public static readonly EventId GetAuthTokenFailed = new(GetAuthTokenFailedId, nameof(GetAuthTokenFailed));

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get auth token for resource {resource}: {message}", EventId = GetAuthTokenFailedId)]
        public static partial void LogGetAuthTokenFailed(this ILogger logger, string resource, string message);

        // Token cache operation failed
        public static readonly EventId TokenCacheFailed = new(TokenCacheFailedId, nameof(TokenCacheFailed));

        [LoggerMessage(Level = LogLevel.Error, Message = "Token cache operation failed for resource {resource}: {message}", EventId = TokenCacheFailedId)]
        public static partial void LogTokenCacheFailed(this ILogger logger, string resource, string message);

        // Token credential acquisition failed
        public static readonly EventId TokenCredentialFailed = new(TokenCredentialFailedId, nameof(TokenCredentialFailed));

        [LoggerMessage(Level = LogLevel.Error, Message = "Token credential acquisition failed for resource {resource}: {exception}", EventId = TokenCredentialFailedId)]
        public static partial void LogTokenCredentialFailed(this ILogger logger, string resource, Exception exception);
    }
}

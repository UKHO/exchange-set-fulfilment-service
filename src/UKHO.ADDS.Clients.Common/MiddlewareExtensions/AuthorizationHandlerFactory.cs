using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    // <summary>
    /// Factory for creating authorization verification handlers
    /// </summary>
    public static class AuthorizationHandlerFactory
    {
        /// <summary>
        /// Creates an authorization header verification handler
        /// </summary>
        public static AuthorizationHeaderVerificationHandler CreateHandler(IServiceProvider serviceProvider, string clientName)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AuthorizationHeaderVerificationHandler>>();
            return new AuthorizationHeaderVerificationHandler(logger, clientName);
        }
    }
}

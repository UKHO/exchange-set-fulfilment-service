using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    public class AuthorizationHeaderVerificationHandler : DelegatingHandler
    {
        private readonly ILogger<AuthorizationHeaderVerificationHandler> _logger;
        private readonly string _clientName;

        public AuthorizationHeaderVerificationHandler(ILogger<AuthorizationHeaderVerificationHandler> logger, string clientName = "Unknown")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Verify authorization before sending request
            VerifyAuthorizationHeader(request);

            var response = await base.SendAsync(request, cancellationToken);

            // Log response status for correlation with auth issues
            LogResponseStatus(response, request.RequestUri);

            return response;
        }

        private void VerifyAuthorizationHeader(HttpRequestMessage request)
        {
            try
            {
                _logger.LogDebug("🔍 [{ClientName}] Verifying authorization for: {Method} {Uri}",
                    _clientName, request.Method, request.RequestUri?.AbsolutePath);

                var authHeader = request.Headers.Authorization;

                if (authHeader == null)
                {
                    _logger.LogWarning("❌ [{ClientName}] No Authorization header found for request to {Uri}",
                        _clientName, request.RequestUri?.AbsolutePath);
                    return;
                }

                var scheme = authHeader.Scheme;
                var hasParameter = !string.IsNullOrEmpty(authHeader.Parameter);

                if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    if (hasParameter)
                    {
                        var tokenInfo = GetSecureTokenInfo(authHeader.Parameter);
                        _logger.LogInformation("✅ [{ClientName}] Bearer token verified: {TokenInfo}",
                            _clientName, tokenInfo);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [{ClientName}] Bearer scheme present but no token parameter", _clientName);
                    }
                }
                else
                {
                    _logger.LogInformation("✅ [{ClientName}] Authorization header present with scheme: {Scheme}",
                        _clientName, scheme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify authorization header for client {ClientName}", _clientName);
            }
        }

        private void LogResponseStatus(HttpResponseMessage response, Uri? requestUri)
        {
            var statusCode = response.StatusCode;
            var path = requestUri?.AbsolutePath ?? "unknown";

            if (statusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("🚫 [{ClientName}] 401 Unauthorized response for {Path} - Authentication failed",
                    _clientName, path);
            }
            else if (statusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError("🚫 [{ClientName}] 403 Forbidden response for {Path} - Authorization failed",
                    _clientName, path);
            }
            else if ((int)statusCode >= 400)
            {
                _logger.LogWarning("⚠️ [{ClientName}] {StatusCode} response for {Path}",
                    _clientName, (int)statusCode, path);
            }
            else
            {
                _logger.LogDebug("✅ [{ClientName}] {StatusCode} response for {Path}",
                    _clientName, (int)statusCode, path);
            }
        }

        private object GetSecureTokenInfo(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return new { Status = "Empty token" };
                }

                // Basic JWT structure check
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    return new
                    {
                        Status = "Invalid format",
                        Length = token.Length,
                        Parts = parts.Length
                    };
                }

                // Safely extract expiration without exposing token content
                var payload = parts[1];
                while (payload.Length % 4 != 0) payload += "=";

                var payloadBytes = Convert.FromBase64String(payload);
                var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

                using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;

                var hasExp = root.TryGetProperty("exp", out var expProp);
                var hasAud = root.TryGetProperty("aud", out _);
                var hasIss = root.TryGetProperty("iss", out _);

                string expirationStatus = "Unknown";
                if (hasExp && expProp.TryGetInt64(out var exp))
                {
                    var expiration = DateTimeOffset.FromUnixTimeSeconds(exp);
                    var timeLeft = expiration - DateTimeOffset.UtcNow;

                    if (timeLeft.TotalSeconds <= 0)
                        expirationStatus = "Expired";
                    else if (timeLeft.TotalMinutes < 5)
                        expirationStatus = "Expires soon";
                    else
                        expirationStatus = "Valid";
                }

                return new
                {
                    Status = "Valid JWT",
                    Length = token.Length,
                    HasExpiration = hasExp,
                    HasAudience = hasAud,
                    HasIssuer = hasIss,
                    ExpirationStatus = expirationStatus
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = "Token analysis failed",
                    Error = ex.Message,
                    Length = token?.Length ?? 0
                };
            }
        }
    }
}

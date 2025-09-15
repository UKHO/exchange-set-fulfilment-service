using Azure.Core;

namespace UKHO.ADDS.Clients.Common.Authentication
{
    public sealed class TokenCredentialAuthenticationTokenProvider : IAuthenticationTokenProvider
    {
        private readonly TokenCredential _credential;
        private readonly TimeSpan _refreshSkew;
        private readonly string[] _scopes;
        private AccessToken _cached;

        public TokenCredentialAuthenticationTokenProvider(TokenCredential credential, IEnumerable<string> scopes, TimeSpan? refreshSkew = null)
        {
            _credential = credential ?? throw new ArgumentNullException(nameof(credential));
            _scopes = (scopes ?? throw new ArgumentNullException(nameof(scopes))).ToArray();
            if (_scopes.Length == 0)
            {
                throw new ArgumentException("At least one scope is required.", nameof(scopes));
            }

            _refreshSkew = refreshSkew ?? TimeSpan.FromMinutes(5);
            _cached = default;
        }

        public async Task<string> GetTokenAsync()
        {
            var now = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(_cached.Token) && _cached.ExpiresOn - _refreshSkew > now)
            {
                return _cached.Token;
            }

            _cached = await _credential.GetTokenAsync(new TokenRequestContext(_scopes), CancellationToken.None).ConfigureAwait(false);
            return _cached.Token;
        }
    }
}

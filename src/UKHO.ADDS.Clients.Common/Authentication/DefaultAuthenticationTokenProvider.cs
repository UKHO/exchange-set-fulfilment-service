namespace UKHO.ADDS.Clients.Common.Authentication
{
    internal class DefaultAuthenticationTokenProvider : IAuthenticationTokenProvider
    {
        private readonly string _accessToken;

        public DefaultAuthenticationTokenProvider(string accessToken) => _accessToken = accessToken;

        public Task<string> GetTokenAsync() => Task.FromResult(_accessToken);
    }
}

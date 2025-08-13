namespace UKHO.ADDS.Clients.Common.Authentication
{
    public sealed class AnonymousAuthenticationTokenProvider : IAuthenticationTokenProvider
    {
        public Task<string> GetTokenAsync() => Task.FromResult(string.Empty);
    }
}

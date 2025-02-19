namespace UKHO.Clients.FileShare
{
    public interface IAuthTokenProvider
    {
        Task<string> GetTokenAsync();
    }

    internal class DefaultAuthTokenProvider : IAuthTokenProvider
    {
        private readonly string _accessToken;

        public DefaultAuthTokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public Task<string> GetTokenAsync()
        {
            return Task.FromResult(_accessToken);
        }
    }
}

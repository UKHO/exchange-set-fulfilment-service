using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly
{
    public class FileShareReadOnlyClientFactory : IFileShareReadOnlyClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public FileShareReadOnlyClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IFileShareReadOnlyClient CreateClient(string baseAddress, string accessToken) => new FileShareReadOnlyClient(_clientFactory, baseAddress, accessToken);

        public IFileShareReadOnlyClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider) => new FileShareReadOnlyClient(_clientFactory, baseAddress, tokenProvider);
    }
}

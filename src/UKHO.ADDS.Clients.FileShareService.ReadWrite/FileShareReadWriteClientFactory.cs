using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public class FileShareReadWriteClientFactory : IFileShareReadWriteClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public FileShareReadWriteClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public IFileShareReadWriteClient CreateClient(string baseAddress, string accessToken) => new FileShareReadWriteClient(_clientFactory, baseAddress, accessToken);

        public IFileShareReadWriteClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider) => new FileShareReadWriteClient(_clientFactory, baseAddress, tokenProvider);
    }
}

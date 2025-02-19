using UKHO.Clients.Common.Configuration;

namespace UKHO.Clients.FileShare
{
    internal class FileShareClientFactory : IFileShareClientFactory
    {
        private readonly ClientConfiguration _configuration;

        public FileShareClientFactory(ClientConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<IFileShareClient> CreateFileShareClientAsync() => Task.FromResult<IFileShareClient>(new DummyFileShareClient(_configuration));
    }
}

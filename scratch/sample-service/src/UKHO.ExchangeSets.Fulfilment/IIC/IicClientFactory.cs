using Microsoft.Extensions.Configuration;
using UKHO.Clients.Common.Configuration;
using UKHO.Clients.FileShare;

namespace UKHO.ExchangeSets.Fulfilment.IIC
{
    internal class IicClientFactory : IIicClientFactory
    {
        private readonly ClientConfiguration _configuration;

        public IicClientFactory(ClientConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<IIicClient> CreateIicClientAsync() => Task.FromResult<IIicClient>(new IicClient(_configuration));
    }
}

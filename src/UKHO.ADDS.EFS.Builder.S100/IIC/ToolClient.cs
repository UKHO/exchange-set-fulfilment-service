using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.EFS.Builder.S100.IIC
{
    internal class ToolClient : IToolClient
    {
        private readonly IHttpClientFactory _clientFactory;

        public ToolClient(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public Task<Result> Ping() => Task.FromResult(Result.Success());
    }
}

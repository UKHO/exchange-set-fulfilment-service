using System.Net.Http.Headers;
using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    internal class NodeStatusWriter : INodeStatusWriter
    {
        private readonly HttpClient _httpClient;

        public NodeStatusWriter(IHttpClientFactory clientFactory) => _httpClient = clientFactory.CreateClient();

        public async Task WriteNodeStatusTelemetry(ExchangeSetBuilderNodeStatus nodeStatus, string buildServiceEndpoint)
        {
            var uri = new Uri(new Uri(buildServiceEndpoint), "/status");

            var json = JsonCodec.Encode(nodeStatus);

            var request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = uri, Content = new StringContent(json) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };

            using var _ = await _httpClient.SendAsync(request);
        }

        public async Task WriteDebugExchangeSetJob(ExchangeSetJob exchangeSetJob, string buildServiceEndpoint)
        {
            var uri = new Uri(new Uri(buildServiceEndpoint), $"/jobs/debug/{exchangeSetJob.Id}");

            var json = JsonCodec.Encode(exchangeSetJob);

            var request = new HttpRequestMessage { Method = HttpMethod.Post, RequestUri = uri, Content = new StringContent(json) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } } };

            using var _ = await _httpClient.SendAsync(request);
        }
    }
}

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UKHO.ADDS.EFS.Trigger
{
    public class OrchestratorJobClient
    {
        private readonly HttpClient _httpClient;

        public OrchestratorJobClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<HttpResponseMessage> PostJobAsync(string orchestratorApiUrl, object jobRequest, string correlationId)
        {
            var requestJson = JsonSerializer.Serialize(jobRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, orchestratorApiUrl)
            {
                Content = content
            };
            request.Headers.Add("X-Correlation-Id", correlationId);

            return await _httpClient.SendAsync(request);
        }
    }
}

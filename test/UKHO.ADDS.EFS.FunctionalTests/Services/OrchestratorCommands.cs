using System.Text;
using System.Text.Json;
using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class OrchestratorCommands
    {
        private static HttpClient httpClient => AspireResourceSingleton.httpClient!;
        private const int WaitDurationMs = 5000;
        private const double MaxWaitMinutes = 5;

        public static async Task<HttpResponseMessage> PostRequestAsync(string requestId, object payload, string endpoint)
        {
            var content = new StringContent(payload is string payloadString ? payloadString : JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            content.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, requestId);

            //Adding a random delay to avoid too many concurrent requests
            await Task.Delay(Random.Shared.Next(5000, 10000));

            var response = await httpClient.PostAsync(endpoint, content);

            return response;
        }


        public static async Task<HttpResponseMessage> WaitForJobCompletionAsync(string jobId)
        {
            var startTime = DateTime.Now;
            HttpResponseMessage response = null!;
            JsonDocument responseJson = null!;
            string jobState;
            do
            {
                response = await httpClient.GetAsync($"/jobs/{jobId}");
                responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                jobState = responseJson.RootElement.GetProperty("jobState").GetString() ?? string.Empty;

                if (jobState is "completed" or "failed")
                {
                    break;
                }

                await Task.Delay(WaitDurationMs);
            } while ((DateTime.Now - startTime).TotalMinutes < MaxWaitMinutes);

            return response;
        }

        public static async Task<HttpResponseMessage> GetBuildStatusAsync(string jobId)
        {
            var response = await httpClient.GetAsync($"/jobs/{jobId}/build");
            return response;
        }
    }
}

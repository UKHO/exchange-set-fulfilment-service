using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

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

        /// <summary>
        /// Submits a productNames endpoint and asserts the requestedProductCount and exchangeSetProductCount.
        /// </summary>
        public static async Task<string> ProductNamesInCustomAssemblyPipelineSubmitJobAsync(HttpClient httpClient, string? callbackUri, object[]? products = null)
        {
            products ??= Array.Empty<string>();
            var requestId = $"job-0001-" + Guid.NewGuid();
            var payload = products;

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            content.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, requestId);

            var response = await httpClient.PostAsync($"/v2/exchangeSet/s100/productNames?callbackUri={callbackUri}", content);

            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var requestedProductCount = responseJson.RootElement.GetProperty("requestedProductCount").GetInt32();
            var exchangeSetProductCount = responseJson.RootElement.GetProperty("exchangeSetProductCount").GetInt32();

            Assert.Equal(products.Length, requestedProductCount);
            Assert.Equal(products.Length, exchangeSetProductCount);

            return requestId!;
        }
    }
}

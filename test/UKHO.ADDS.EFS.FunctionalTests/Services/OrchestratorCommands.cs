using System.Text;
using System.Text.Json;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Kiota.SalesCatalogueService.Models;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class OrchestratorCommands
    {
        private const int WaitDurationMs = 2000;
        private const double MaxWaitMinutes = 2;

        /// <summary>
        /// Submits a job and asserts the expected job and build status.
        /// </summary>
        public static async Task<string> SubmitJobAsync(HttpClient httpClient, string filter = "", string[]? products = null, string expectedJobStatus = "submitted", string expectedBuildStatus = "scheduled")
        {
            products ??= new [] { "" };
            var requestId = $"job-0001-" + Guid.NewGuid();
            var payload = new { dataStandard = "s100", products = products, filter = $"{filter}" };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            content.Headers.Add(ApiHeaderKeys.XCorrelationIdHeaderKey, requestId);

            var response = await httpClient.PostAsync("/jobs", content);

            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var jobStatus = responseJson.RootElement.GetProperty("jobStatus").GetString();
            var buildStatus = responseJson.RootElement.GetProperty("buildStatus").GetString();

            Assert.Equal(expectedJobStatus, jobStatus);
            Assert.Equal(expectedBuildStatus, buildStatus);

            return requestId!;
        }

        /// <summary>
        /// Waits for a job to complete and asserts the final state.
        /// </summary>
        public static async Task WaitForJobCompletionAsync(HttpClient httpClient, string jobId)
        {
            var startTime = DateTime.Now;

            string jobState, buildState;
            do
            {
                using var response = await httpClient.GetAsync($"/jobs/{jobId}");
                var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                jobState = responseJson.RootElement.GetProperty("jobState").GetString() ?? string.Empty;
                buildState = responseJson.RootElement.GetProperty("buildState").GetString() ?? string.Empty;

                if (jobState is "completed" or "failed")
                    break;

                await Task.Delay(WaitDurationMs);
            } while ((DateTime.Now - startTime).TotalMinutes < MaxWaitMinutes);

            Assert.Equal("completed", jobState);
            Assert.Equal("succeeded", buildState);
        }

        /// <summary>
        /// Verifies the build status of a job.
        /// </summary>
        public static async Task VerifyBuildStatusAsync(HttpClient httpClient, string jobId)
        {
            using var response = await httpClient.GetAsync($"/jobs/{jobId}/build");
            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got: {response.StatusCode}");

            var responseJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var builderExitCode = responseJson.RootElement.GetProperty("builderExitCode").GetString();

            Assert.Equal("success", builderExitCode);
        }

        /// <summary>
        /// Verifies the product names endpoint response.
        /// </summary>
        public static async Task VerifyProductNamesEndpointResponse(object productNames, HttpClient httpClient, string? callbackUri, HttpStatusCode expectedStatusCode, string expectedErrorMessage, int jobNumber = 1, bool includeRequestId = true)
        {
            var requestId = includeRequestId ? $"job-000{jobNumber}-" + Guid.NewGuid() : string.Empty;

            var content = new StringContent(JsonSerializer.Serialize(productNames), Encoding.UTF8, "application/json");

            if (includeRequestId)
            {
                content.Headers.Add("x-correlation-id", requestId);
            }

            var response = await httpClient.PostAsync($"/v2/exchangeSet/s100/productNames?callbackUri={callbackUri}", content);

            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }

        /// <summary>
        /// Verifies the product version endpoint response.
        /// </summary>
        public static async Task VerifyProductVersionEndpointResponse(string productVersion, string callbackUri, HttpClient httpClient,
            HttpStatusCode expectedStatusCode, string expectedErrorMessage, int jobNumber = 1)
        {
            var requestId = $"job-000{jobNumber}-" + Guid.NewGuid();

            var content = new StringContent(productVersion, Encoding.UTF8, "application/json");

            content.Headers.Add("x-correlation-id", requestId);

            // Send the POST request
            var response = await httpClient.PostAsync($"/v2/exchangeSet/s100/productVersions?callbackUri={callbackUri}", content);

            // Validate the response status code
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Assert.Contains(expectedErrorMessage, responseBody);
            }
        }

        /// <summary>
        /// Verifies the Update Since endpoint response.
        /// </summary>
        public static async Task VerifyUpdateSinceEndpointResponse(string sinceDateTime, string callbackUri, string productIdentifier,
            HttpClient httpClient, HttpStatusCode expectedStatusCode, string expectedErrorMessage, int jobNumber = 1)
        {
            var requestId = $"job-000{jobNumber}-" + Guid.NewGuid();

            var requestPayload = $"{{ \"sinceDateTime\": \"{sinceDateTime}\" }}";

            var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");

            content.Headers.Add("x-correlation-id", requestId);

            // Send the POST request
            var response = await httpClient.PostAsync($"/v2/exchangeSet/s100/updatesSince?callbackUri={callbackUri}&productIdentifier={productIdentifier}", content);
            
            // Validate the response status code
            Assert.Equal(expectedStatusCode, response.StatusCode);

            if (expectedStatusCode != HttpStatusCode.Accepted && expectedErrorMessage != "")
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Assert.Contains(expectedErrorMessage, responseBody);
            }
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

            Assert.Equal(products.Length , requestedProductCount);
            Assert.Equal(products.Length, exchangeSetProductCount);

            return requestId!;
        }
    }
}

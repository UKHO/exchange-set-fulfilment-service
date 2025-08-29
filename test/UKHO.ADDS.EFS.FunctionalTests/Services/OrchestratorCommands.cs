using System.Text;
using System.Text.Json;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class OrchestratorCommands
    {
        private const int WaitDurationMs = 2000;
        private const double MaxWaitMinutes = 2;

        /// <summary>
        /// Submits a job and asserts the expected job and build status.
        /// </summary>
        public static async Task<string> SubmitJobAsync(HttpClient httpClient, string filter = "", int jobNumber = 1, string[]? products = null, string expectedJobStatus = "submitted", string expectedBuildStatus = "scheduled")
        {
            var requestId = $"job-000{jobNumber}-" + Guid.NewGuid();
            var payload = new { version = 1, dataStandard = "s100", products = products, filter = $"{filter}" };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            content.Headers.Add("x-correlation-id", requestId);

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
    }
}

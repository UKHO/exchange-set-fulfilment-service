using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.EndToEnd_Tests.Tests
{
    public class EndToEndTests : IAsyncLifetime
    {
        private DistributedApplication _app;


        public async Task InitializeAsync()
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UKHO_ADDS_EFS_LocalHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            _app = await appHost.BuildAsync();

            var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
            await _app.StartAsync();
            await resourceNotificationService.WaitForResourceAsync(ProcessNames.OrchestratorService, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

        }

        public async Task DisposeAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }



        [Fact]
        public async Task S100EndToEnd()
        {

            // Act
            var httpClient = _app.CreateHttpClient(ProcessNames.OrchestratorService);


            // 1.Prepare a job submission request and confirm that it was submitted successfully.
            var content = new StringContent(
                """
                {
                    "version": 1,
                    "dataStandard": "s100",
                    "products": "",
                    "filter": ""
                }
                """,
                Encoding.UTF8, "application/json");
            content.Headers.Add("x-correlation-id", "a-test-job-0001");

            var jobSubmitResponse = await httpClient.PostAsync("/jobs", content);
            Assert.True(jobSubmitResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobSubmitResponse.StatusCode);
            var responseContent = await jobSubmitResponse.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseContent);
            responseJson.RootElement.TryGetProperty("jobId", out var jobId);
            responseJson.RootElement.TryGetProperty("jobStatus", out var jobStatus);
            responseJson.RootElement.TryGetProperty("buildStatus", out var buildStatus);

            Assert.Equal("submitted", jobStatus.GetString());
            Assert.Equal("scheduled", buildStatus.GetString());

            // 2.Check for notification that the job has been picked up by the builder and completed successfully. 
            string currentJobState;
            string currentBuildState;
            double elapsedMinutes = 0;
            var waitDuration = 2000; // 2 seconds
            var maxTimeToWait = 2; // 2 minutes
            TimeOnly startTime = TimeOnly.FromDateTime(DateTime.Now);
            do
            {
                var jobStateResponse = await httpClient.GetAsync($"/jobs/{jobId}");
                responseContent = await jobStateResponse.Content.ReadAsStringAsync();
                responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("jobState", out var jobState);
                responseJson.RootElement.TryGetProperty("buildState", out var buildState);
                currentJobState = jobState.GetString() ?? string.Empty;
                currentBuildState = buildState.GetString() ?? string.Empty;
                elapsedMinutes = (TimeOnly.FromDateTime(DateTime.Now) - startTime).TotalMinutes;
                await Task.Delay(waitDuration);
            } while (currentJobState == "submitted" && elapsedMinutes < maxTimeToWait);

            Assert.Equal("completed", currentJobState);
            Assert.Equal("succeeded", currentBuildState);

            // 3.Check the builder has successfully returned build status
            var jobCompletedResponse = await httpClient.GetAsync($"/jobs/{jobId}/build");
            Assert.True(jobCompletedResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobCompletedResponse.StatusCode);

            // and that the builder exit code is 'success' although success is not necessary
            // the fact that a response was returned is sufficient to indicate that all components in the process
            // are working together.
            responseContent = await jobCompletedResponse.Content.ReadAsStringAsync();
            responseJson = JsonDocument.Parse(responseContent);
            responseJson.RootElement.TryGetProperty("builderExitCode", out var builderExitCode);
            Assert.Equal("success", builderExitCode.GetString());
        }

        [Fact]
        public async Task RunMultipleRequests()
        {
            var httpClient = _app.CreateHttpClient(ProcessNames.OrchestratorService);

            var content = new StringContent(
                """
                {
                    "version": 1,
                    "dataStandard": "s100",
                    "products": "",
                    "filter": ""
                }
                """,
                Encoding.UTF8, "application/json");
            content.Headers.Add("x-correlation-id", "a-test-job-0001");

            var jobs = new List<string>();
            var completedJobs = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                // Act
                var jobSubmitResponse = await httpClient.PostAsync("/jobs", content);
                Assert.True(jobSubmitResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobSubmitResponse.StatusCode);

                var responseContent = await jobSubmitResponse.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("jobId", out var jobId);
                jobs.Add(jobId.GetString() ?? string.Empty);
                await Task.Delay(1000);
            }

            do { 
                foreach (var jobId in jobs)
                {
                    if (completedJobs.Contains(jobId)) continue; // Skip if job already completed
                    var jobStateResponse = await httpClient.GetAsync($"/jobs/{jobId}");
                    var responseContent = await jobStateResponse.Content.ReadAsStringAsync();
                    var responseJson = JsonDocument.Parse(responseContent);
                    responseJson.RootElement.TryGetProperty("jobState", out var jobState);
                    responseJson.RootElement.TryGetProperty("buildState", out var buildState);
                    if (jobState.GetString() == "completed" && buildState.GetString() == "succeeded")
                    {
                        completedJobs.Add(jobId);
                    }
                }
                // Wait for a short period before checking again
                await Task.Delay(2000);
            } while (completedJobs.Count < jobs.Count);

            Assert.Equal(jobs.Count, completedJobs.Count);

            foreach (var jobId in completedJobs)
            {
                var jobCompletedResponse = await httpClient.GetAsync($"/jobs/{jobId}/build");
                Assert.True(jobCompletedResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobCompletedResponse.StatusCode);
                var responseContent = await jobCompletedResponse.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("builderExitCode", out var builderExitCode);
                Assert.Equal("success", builderExitCode.GetString());
            }


        }


    }
}

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.EndToEndTests
{
    
    public class EndToEndTests : IAsyncLifetime
    {
        private DistributedApplication? _app;
        private bool _isRunningInPipeline = IsRunningInPipeline();
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient? _httpClient = null;
        private readonly IConfiguration _config;

        public EndToEndTests()
        {
            var builder = new ConfigurationBuilder()
                        .AddEnvironmentVariables();
            _config = builder.Build();

            var service = new ServiceCollection();
            service.AddHttpClient("TestClient", client =>
            {
                client.BaseAddress = new Uri($"https://efs-orchestrator.{_config["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]}");
            });

            var serviceProvider = service.BuildServiceProvider();
            _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
        }


        public async Task InitializeAsync()
        {
            if (_isRunningInPipeline)
            {
                _httpClient = _httpClientFactory.CreateClient("TestClient");
            }
            else
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

                _httpClient = _app.CreateHttpClient(ProcessNames.OrchestratorService);
            }
                

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
            var requestId = Guid.NewGuid().ToString();
            content.Headers.Add("x-correlation-id", $"job-0001-{requestId}");


            var jobSubmitResponse = await _httpClient!.PostAsync("/jobs", content);
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
                var jobStateResponse = await _httpClient.GetAsync($"/jobs/{jobId}");
                responseContent = await jobStateResponse.Content.ReadAsStringAsync();
                responseJson = JsonDocument.Parse(responseContent);
                responseJson.RootElement.TryGetProperty("jobState", out var jobState);
                responseJson.RootElement.TryGetProperty("buildState", out var buildState);
                currentJobState = jobState.GetString() ?? string.Empty;
                currentBuildState = buildState.GetString() ?? string.Empty;
                await Task.Delay(waitDuration);
                elapsedMinutes = (TimeOnly.FromDateTime(DateTime.Now) - startTime).TotalMinutes;
            } while (currentJobState == "submitted" && elapsedMinutes < maxTimeToWait);

            Assert.Equal("completed", currentJobState);
            Assert.Equal("succeeded", currentBuildState);

            // 3.Check the builder has returned build status and it has been successfully processed by orchestrator.
            var jobCompletedResponse = await _httpClient.GetAsync($"/jobs/{jobId}/build");
            Assert.True(jobCompletedResponse.IsSuccessStatusCode, "Expected success status code but got: " + jobCompletedResponse.StatusCode);

            // and that the builder exit code is 'success' although success is not necessary
            // the fact that a response was returned is sufficient to indicate that all components in the process
            // are working together.
            responseContent = await jobCompletedResponse.Content.ReadAsStringAsync();
            responseJson = JsonDocument.Parse(responseContent);
            responseJson.RootElement.TryGetProperty("builderExitCode", out var builderExitCode);
            Assert.Equal("success", builderExitCode.GetString());

            
        }

        private static bool IsRunningInPipeline()
        {
            // Common environment variables for CI/CD pipelines
            var ci = Environment.GetEnvironmentVariable("CI");
            var tfBuild = Environment.GetEnvironmentVariable("TF_BUILD");
            var githubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            var azurePipeline = Environment.GetEnvironmentVariable("AGENT_NAME");

            return !string.IsNullOrEmpty(ci)
                || !string.IsNullOrEmpty(tfBuild)
                || !string.IsNullOrEmpty(githubActions)
                || !string.IsNullOrEmpty(azurePipeline);


        }


    }
}

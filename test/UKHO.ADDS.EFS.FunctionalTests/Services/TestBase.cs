using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class TestBase : IAsyncLifetime
    {
        protected DistributedApplication? App;
        private bool _isRunningInPipeline = IsRunningInPipeline();
        protected HttpClient httpClient;
        protected HttpClient httpClientMock;

        // Configuration settings for pipeline running
        private IConfiguration? _configuration;
        public static string? ProjectDirectory;

        public TestBase()
        {
            ProjectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        }

        public async Task InitializeAsync()
        {
            if (_isRunningInPipeline)
            {
                var builder = new ConfigurationBuilder()
                        .AddEnvironmentVariables();
                _configuration = builder.Build();

                var orchestratorUrl = _configuration["ORCHESTRATOR_URL"] ?? throw new ArgumentNullException("Orchestrator Url");
                var mockUrl = _configuration["ADDSMOCK_URL"] ?? throw new ArgumentNullException("Mock Url");

                httpClient = new HttpClient
                {
                    BaseAddress = new Uri(orchestratorUrl)
                };
                httpClientMock = new HttpClient
                {
                    BaseAddress = new Uri(mockUrl)
                };

            }
            else
            {
                var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UKHO_ADDS_EFS_LocalHost>();
                appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
                {
                    clientBuilder.AddStandardResilienceHandler();
                });
                App = await appHost.BuildAsync();

                var resourceNotificationService = App.Services.GetRequiredService<ResourceNotificationService>();
                await App.StartAsync();
                await resourceNotificationService
                    .WaitForResourceAsync(ProcessNames.OrchestratorService, KnownResourceStates.Running)
                    .WaitAsync(TimeSpan.FromSeconds(30));

                httpClient = App.CreateHttpClient(ProcessNames.OrchestratorService);
                httpClientMock = App.CreateHttpClient(ProcessNames.MockService);
            }

        }

        public async Task DisposeAsync()
        {
            //Clean up temporary files and directories
            var outDir = Path.Combine(ProjectDirectory!, "out");

            if (Directory.Exists(outDir))
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);

            if (_isRunningInPipeline)
            {
                return; // No need to dispose in pipeline, as it is managed by the CI/CD environment
            }

            if (App != null)
            {
                await App.StopAsync();
                await App.DisposeAsync();
            }

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

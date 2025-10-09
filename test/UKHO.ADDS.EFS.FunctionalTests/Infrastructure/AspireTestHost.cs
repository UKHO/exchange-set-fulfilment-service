using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Infrastructure
{
    public class AspireTestHost : IAsyncDisposable
    {
        private readonly bool _isRunningInPipeline = IsRunningInPipeline();

        // Configuration settings for pipeline running
        private IConfiguration? _configuration;

        private static readonly Lazy<Task<AspireTestHost>> _instance = new(() => CreateAsync());

        public static Task<AspireTestHost> Instance => _instance.Value;

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private AspireTestHost() { }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private static async Task<AspireTestHost> CreateAsync()
        {
            var singleton = new AspireTestHost();
            await singleton.InitializeAsync();
            return singleton;
        }

        public static DistributedApplication? App { get; private set; }
        public static HttpClient? httpClient { get; private set; }
        public static HttpClient? httpClientMock { get; private set; }
        public static string ProjectDirectory { get; } = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;


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


        // CA1822 suppression comment
        //[System.Diagnostics.CodeAnalysis.SuppressMessage]
        private async Task InitializeAsync()
        {
            if (_isRunningInPipeline)
            {
                var builder = new ConfigurationBuilder().AddEnvironmentVariables();
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
                    .WaitAsync(TimeSpan.FromSeconds(60));

                await Task.Delay(10000);

                httpClient = App.CreateHttpClient(ProcessNames.OrchestratorService);
                httpClientMock = App.CreateHttpClient(ProcessNames.MockService);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Disposing AspireTestHost");

            if (_isRunningInPipeline)
            {
                // No need to dispose App in pipeline, as it is managed by the CI/CD environment
            }
            else if (App != null)
            {
                await App.StopAsync();
                await App.DisposeAsync();
            }

            //Clean up temporary files and directories
            var outDir = Path.Combine(ProjectDirectory!, "out");

            if (Directory.Exists(outDir))
            {
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);
                Array.ForEach(Directory.GetFiles(outDir, "*.txt"), File.Delete);
            }

            GC.SuppressFinalize(this);
        }
    }
}

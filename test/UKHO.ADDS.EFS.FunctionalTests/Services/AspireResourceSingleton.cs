using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class AspireResourceSingleton : IAsyncDisposable
    {
        private bool _isRunningInPipeline = IsRunningInPipeline();

        // Configuration settings for pipeline running
        private IConfiguration? _configuration;

        private static readonly Lazy<Task<AspireResourceSingleton>> _instance = new(() => CreateAsync());

        public static Task<AspireResourceSingleton> Instance => _instance.Value;

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private AspireResourceSingleton() { }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private static async Task<AspireResourceSingleton> CreateAsync()
        {
            var singleton = new AspireResourceSingleton();
            await singleton.InitializeAsync();
            return singleton;
        }

        public static DistributedApplication? App { get; private set; }
        public static HttpClient? httpClient { get; private set; }
        public static HttpClient? httpClientMock { get; private set; }
        public static string ProjectDirectory { get; } = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;


        private HttpClient ConfigureHttpClientWithResilience(HttpClient client)
        {
            // Create a resilience policy that includes:
            // 1. Retry with exponential backoff for transient failures
            // 2. Circuit breaker to prevent overwhelming failing services
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));

            // Apply policies to the HttpClient
            var policyHandler = new PolicyHttpMessageHandler(retryPolicy);
            policyHandler.InnerHandler = new HttpClientHandler();

            var circuitBreakerHandler = new PolicyHttpMessageHandler(circuitBreakerPolicy);
            circuitBreakerHandler.InnerHandler = policyHandler;

            // Create a new client with the policy handlers
            var clientWithResilience = new HttpClient(circuitBreakerHandler)
            {
                BaseAddress = client.BaseAddress,
                Timeout = TimeSpan.FromMinutes(5) // Increase default timeout for long-running operations
            };

            // Copy any headers or default request headers
            foreach (var header in client.DefaultRequestHeaders)
            {
                clientWithResilience.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return clientWithResilience;
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

                // Create basic HttpClients
                var basicHttpClient = new HttpClient
                {
                    BaseAddress = new Uri(orchestratorUrl)
                };

                var basicHttpClientMock = new HttpClient
                {
                    BaseAddress = new Uri(mockUrl)
                };

                // Apply resilience patterns to the clients
                httpClient = ConfigureHttpClientWithResilience(basicHttpClient);
                httpClientMock = ConfigureHttpClientWithResilience(basicHttpClientMock);

                // Dispose the basic clients as they're no longer needed
                basicHttpClient.Dispose();
                basicHttpClientMock.Dispose();
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
            }

            GC.SuppressFinalize(this);
        }
    }
}

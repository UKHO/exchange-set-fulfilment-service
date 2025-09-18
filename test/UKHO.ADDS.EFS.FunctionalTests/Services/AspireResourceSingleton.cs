using Aspire.Hosting;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class AspireResourceSingleton : IAsyncDisposable
    {
        private static readonly Lazy<Task<AspireResourceSingleton>> _instance =
            new(() => CreateAsync());

        public static Task<AspireResourceSingleton> Instance => _instance.Value;

        private AspireResourceSingleton() { }

        private static async Task<AspireResourceSingleton> CreateAsync()
        {
            var singleton = new AspireResourceSingleton();
            await singleton.InitializeAsync();
            return singleton;
        }

        public static DistributedApplication? App { get; private set; }
        public static string ProjectDirectory { get; } = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;

        // CA1822 suppression comment
        //[System.Diagnostics.CodeAnalysis.SuppressMessage]
        private async Task InitializeAsync()
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

            await Task.Delay(10000);
        }

        public async ValueTask DisposeAsync()
        {
            if (App != null)
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

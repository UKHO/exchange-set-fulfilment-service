using Aspire.Hosting;
using UKHO.ADDS.EFS.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    public class TestBase : IAsyncLifetime
    {
        protected DistributedApplication? App;
        public static string? ProjectDirectory;

        public TestBase()
        {
            ProjectDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        }

        public async Task InitializeAsync()
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
        }

        public async Task DisposeAsync()
        {
            if (App != null)
            {
                await App.StopAsync();
                await App.DisposeAsync();
            }

            //Clean up temporary files and directories
            var outDir = Path.Combine(ProjectDirectory!, "out");

            if (Directory.Exists(outDir))
                Array.ForEach(Directory.GetFiles(outDir, "*.zip"), File.Delete);

        }
    }
}

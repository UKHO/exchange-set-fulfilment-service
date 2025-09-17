using System.Runtime.CompilerServices;

[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 0)]
[assembly: TestFramework("Meziantou.Xunit.ParallelTestFramework", "Meziantou.Xunit.ParallelTestFramework")]

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    [CollectionDefinition("Startup")]
    public class StartupCollection : ICollectionFixture<StartupFixture> { }

    public class StartupFixture : IAsyncLifetime
    {
        private AspireResourceSingleton? _singleton;

        public async Task InitializeAsync()
        {
            // Initialize the singleton once for all tests
            _singleton = await AspireResourceSingleton.Instance;
        }

        public async Task DisposeAsync()
        {
            if (_singleton != null)
            {
                await _singleton.DisposeAsync();
            }
        }
    }
}

using Meziantou.Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = false, MaxParallelThreads = 0)]

namespace UKHO.ADDS.EFS.FunctionalTests.Services
{
    [CollectionDefinition("Startup Collection")]
    [EnableParallelization] // This enables the parallel execution of classes in a collection 
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
            Console.WriteLine("Disposing AssemblySetup");

            if (_singleton != null)
            {
                await _singleton.DisposeAsync();
            }
        }
    }
}

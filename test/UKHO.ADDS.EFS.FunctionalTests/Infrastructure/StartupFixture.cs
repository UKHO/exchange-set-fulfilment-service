namespace UKHO.ADDS.EFS.FunctionalTests.Infrastructure
{
    public class StartupFixture : IAsyncLifetime
    {
        private AspireTestHost? _singleton;

        public async Task InitializeAsync()
        {
            // Initialize the singleton once for all tests
            _singleton = await AspireTestHost.Instance;
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

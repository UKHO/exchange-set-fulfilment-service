using Meziantou.Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = false, MaxParallelThreads = -1)]

namespace UKHO.ADDS.EFS.FunctionalTests.Infrastructure
{
    [EnableParallelization] // This enables the parallel execution of classes in a collection
    [CollectionDefinition("Startup Collection")]
    public class StartupCollection : ICollectionFixture<StartupFixture> { }
}

using Meziantou.Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace UKHO.ADDS.EFS.FunctionalTests.Infrastructure
{
    // currently dissabled parallel test runs due to test failing on dev pipeline
    // [EnableParallelization] // This enables the parallel execution of classes in a collection
    [CollectionDefinition("Startup Collection")]
    public class StartupCollection : ICollectionFixture<StartupFixture> { }
}

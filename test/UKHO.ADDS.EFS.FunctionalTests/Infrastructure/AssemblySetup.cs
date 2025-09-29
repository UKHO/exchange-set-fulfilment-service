using Meziantou.Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = false, MaxParallelThreads = 0)]

namespace UKHO.ADDS.EFS.FunctionalTests.Infrastructure
{
    [CollectionDefinition("Startup Collection")]
    [EnableParallelization] // This enables the parallel execution of classes in a collection 
    public class StartupCollection : ICollectionFixture<StartupFixture> { }
}

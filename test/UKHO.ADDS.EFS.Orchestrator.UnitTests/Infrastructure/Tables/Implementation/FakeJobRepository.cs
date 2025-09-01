using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeJobRepository : FakeRepository<Job>
    {
        public FakeJobRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.Id, x => (string)x.Id)
        {
        }
    }
}

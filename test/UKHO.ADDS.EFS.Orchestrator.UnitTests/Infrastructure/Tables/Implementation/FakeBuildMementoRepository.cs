using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeBuildMementoRepository : FakeRepository<BuildMemento>
    {
        public FakeBuildMementoRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}

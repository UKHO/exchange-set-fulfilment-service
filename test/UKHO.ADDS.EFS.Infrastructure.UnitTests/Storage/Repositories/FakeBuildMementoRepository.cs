using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Storage.Repositories
{
    public class FakeBuildMementoRepository : FakeRepository<BuildMemento>
    {
        public FakeBuildMementoRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}

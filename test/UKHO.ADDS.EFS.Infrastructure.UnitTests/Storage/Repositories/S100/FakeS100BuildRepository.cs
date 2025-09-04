using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Storage.Repositories.S100
{
    public class FakeS100BuildRepository : FakeRepository<S100Build>
    {
        public FakeS100BuildRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}

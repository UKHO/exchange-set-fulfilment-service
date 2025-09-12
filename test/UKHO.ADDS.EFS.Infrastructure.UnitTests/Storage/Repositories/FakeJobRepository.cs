using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Storage.Repositories
{
    public class FakeJobRepository : FakeRepository<Job>
    {
        public FakeJobRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.Id, x => (string)x.Id)
        {
        }
    }
}

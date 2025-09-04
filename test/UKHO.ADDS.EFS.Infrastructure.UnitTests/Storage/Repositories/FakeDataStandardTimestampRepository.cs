using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Domain.Services.UnitTests.Storage.Repositories;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Infrastructure.UnitTests.Storage.Repositories
{
    public class FakeDataStandardTimestampRepository : FakeRepository<DataStandardTimestamp>
    {
        public FakeDataStandardTimestampRepository()
            : base(StorageConfiguration.S100BuildContainer, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}

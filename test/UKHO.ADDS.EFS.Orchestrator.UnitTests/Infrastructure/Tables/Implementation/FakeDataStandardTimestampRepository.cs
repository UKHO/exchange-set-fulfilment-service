using UKHO.ADDS.EFS.Domain.Products;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeDataStandardTimestampRepository : FakeRepository<DataStandardTimestamp>
    {
        public FakeDataStandardTimestampRepository()
            : base(StorageConfiguration.S100BuildContainer, x => x.DataStandard.ToString().ToLowerInvariant(), x => x.DataStandard.ToString().ToLowerInvariant())
        {
        }
    }
}

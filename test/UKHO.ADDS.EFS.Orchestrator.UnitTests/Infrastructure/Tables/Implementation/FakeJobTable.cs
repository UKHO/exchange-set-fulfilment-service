using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeJobTable : FakeTable<Job>
    {
        public FakeJobTable()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.Id, x => (string)x.Id)
        {
        }
    }
}

using UKHO.ADDS.EFS.Domain.Builds;
using UKHO.ADDS.EFS.Domain.Services.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeBuildMementoTable : FakeTable<BuildMemento>
    {
        public FakeBuildMementoTable()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}

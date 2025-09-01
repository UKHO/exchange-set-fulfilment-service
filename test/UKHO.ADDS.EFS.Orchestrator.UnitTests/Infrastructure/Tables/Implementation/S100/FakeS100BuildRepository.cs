using Azure.Storage.Blobs;
using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation.S100
{
    public class FakeS100BuildRepository : FakeRepository<S100Build>
    {
        public FakeS100BuildRepository()
            : base(StorageConfiguration.S100BuildContainer, x => (string)x.JobId, x => (string)x.JobId)
        {
        }
    }
}

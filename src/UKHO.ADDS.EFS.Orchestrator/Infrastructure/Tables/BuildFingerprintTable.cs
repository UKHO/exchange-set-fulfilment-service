using Azure.Data.Tables;
using UKHO.ADDS.EFS.Configuration.Namespaces;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables.Implementation;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Tables
{
    internal class BuildFingerprintTable : StructuredTable<BuildFingerprint>
    {
        // TODO Swap out for history interface

        public BuildFingerprintTable(TableServiceClient tableServiceClient)
            : base(StorageConfiguration.BuildFingerprintTable, tableServiceClient, x => x.Discriminant, x => x.Discriminant)
        {
        }
    }
}

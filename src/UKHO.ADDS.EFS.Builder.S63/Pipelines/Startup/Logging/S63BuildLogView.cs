using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Domain.Jobs;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup.Logging
{
    internal class S63BuildLogView
    {
        public required JobId Id { get; init; }

        public required DateTime? SalesCatalogueTimestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required int ProductCount { get; init; }

        public static S63BuildLogView CreateFromJob(S63Build build)
        {
            return new S63BuildLogView()
            {
                Id = build.JobId,
                SalesCatalogueTimestamp = build.SalesCatalogueTimestamp,
                DataStandard = build.DataStandard,
                ProductCount = build.GetProductCount()
            };
        }
    }
}

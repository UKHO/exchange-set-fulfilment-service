using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging
{
    internal class S57BuildLogView
    {
        public required JobId Id { get; init; }

        public required DateTime? SalesCatalogueTimestamp { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required int ProductCount { get; init; }

        public static S57BuildLogView CreateFromBuild(S57Build build) =>
            new()
            {
                Id = build.JobId,
                SalesCatalogueTimestamp = build.SalesCatalogueTimestamp,
                DataStandard = build.DataStandard,
                ProductCount = build.GetProductCount()
            };
    }
}

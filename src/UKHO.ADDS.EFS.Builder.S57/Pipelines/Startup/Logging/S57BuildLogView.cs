using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.NewEFS.S57;

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines.Startup.Logging
{
    internal class S57BuildLogView
    {
        public required string Id { get; init; }

        public required DateTime? SalesCatalogueTimestamp { get; init; }

        public required BuildState State { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required int ProductCount { get; init; }

        public static S57BuildLogView CreateFromBuild(S57Build build) =>
            new()
            {
                Id = build.JobId,
                SalesCatalogueTimestamp = build.SalesCatalogueTimestamp,
                State = build.BuildState,
                DataStandard = build.DataStandard,
                ProductCount = build.GetProductCount()
            };
    }
}

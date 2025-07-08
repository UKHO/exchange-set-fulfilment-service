using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.NewEFS.S100;

namespace UKHO.ADDS.EFS.Builder.S100.Pipelines.Startup.Logging
{
    internal class S100BuildLogView
    {
        public required string Id { get; init; }

        public required DateTime? SalesCatalogueTimestamp { get; init; }

        public required BuildState State { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required int ProductCount { get; init; }

        public static S100BuildLogView CreateFromBuild(S100Build build)
        {
            return new S100BuildLogView()
            {
                Id = build.JobId,
                SalesCatalogueTimestamp = build.SalesCatalogueTimestamp,
                State = build.BuildState,
                DataStandard = build.DataStandard,
                ProductCount = build.Products?.Count() ?? 0
            };
        }
    }
}

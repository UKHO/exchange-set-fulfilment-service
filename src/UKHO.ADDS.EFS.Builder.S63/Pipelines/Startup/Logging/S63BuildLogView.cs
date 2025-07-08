using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.NewEFS.S63;

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines.Startup.Logging
{
    internal class S63BuildLogView
    {
        public required string Id { get; init; }

        public required DateTime? SalesCatalogueTimestamp { get; init; }

        public required BuildState State { get; init; }

        public required DataStandard DataStandard { get; init; }

        public required int ProductCount { get; init; }

        public static S63BuildLogView CreateFromJob(S63Build build)
        {
            return new S63BuildLogView()
            {
                Id = build.JobId,
                SalesCatalogueTimestamp = build.SalesCatalogueTimestamp,
                State = build.BuildState,
                DataStandard = build.DataStandard,
                ProductCount = build.GetProductCount()
            };
        }
    }
}

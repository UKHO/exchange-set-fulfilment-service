using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builder.Common.Pipelines;
using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Jobs.S57;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S57.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class S57ExchangeSetPipelineContext : ExchangeSetPipelineContext<S57ExchangeSetJob, S57BuildSummary>
    {
        public S57ExchangeSetPipelineContext(
            IConfiguration configuration,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
            : base(configuration, queueClientFactory, blobClientFactory, loggerFactory)
        {
        }
    }
}

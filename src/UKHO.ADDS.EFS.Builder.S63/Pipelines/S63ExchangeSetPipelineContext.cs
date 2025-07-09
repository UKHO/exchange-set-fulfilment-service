using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.EFS.Builder.Common.Factories;
using UKHO.ADDS.EFS.Builder.Common.Pipelines;
using UKHO.ADDS.EFS.Builds.S63;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace UKHO.ADDS.EFS.Builder.S63.Pipelines
{
    [ExcludeFromCodeCoverage]
    internal class S63ExchangeSetPipelineContext : ExchangeSetPipelineContext<S63Build>
    {
        
        public S63ExchangeSetPipelineContext(
            IConfiguration configuration,
            QueueClientFactory queueClientFactory,
            BlobClientFactory blobClientFactory,
            ILoggerFactory loggerFactory)
            : base(configuration, queueClientFactory, blobClientFactory, loggerFactory)
        {
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using UKHO.ADDS.EFS.Domain.Builds.S63;
using UKHO.ADDS.EFS.Infrastructure.Builders.Factories;
using UKHO.ADDS.EFS.Infrastructure.Builders.Pipelines;


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

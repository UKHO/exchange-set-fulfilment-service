using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    public interface INodeStatusWriter
    {
        Task WriteNodeStatusTelemetry(ExchangeSetBuilderNodeStatus nodeStatus, string buildServiceEndpoint);

        Task WriteDebugExchangeSetJob(ExchangeSetJob job, string buildServiceEndpoint);
    }
}

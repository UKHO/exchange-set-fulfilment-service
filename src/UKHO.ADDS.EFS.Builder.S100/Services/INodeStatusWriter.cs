using UKHO.ADDS.EFS.Entities;

namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    public interface INodeStatusWriter
    {
        Task WriteNodeStatusTelemetry(ExchangeSetBuilderNodeStatus nodeStatus, string buildServiceEndpoint);

        Task WriteDebugExchangeSetJob(ExchangeSetJob job, string buildServiceEndpoint);
    }
}

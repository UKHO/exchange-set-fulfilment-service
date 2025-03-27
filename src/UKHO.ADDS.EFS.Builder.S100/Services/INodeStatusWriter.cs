using UKHO.ADDS.EFS.Common.Entities;

namespace UKHO.ADDS.EFS.Builder.S100.Services
{
    public interface INodeStatusWriter
    {
        Task WriteNodeStatusTelemetry(ExchangeSetBuilderNodeStatus nodeStatus, string buildServiceEndpoint);

        Task WriteDebugExchangeSetRequest(ExchangeSetRequest request, string buildServiceEndpoint);
    }
}

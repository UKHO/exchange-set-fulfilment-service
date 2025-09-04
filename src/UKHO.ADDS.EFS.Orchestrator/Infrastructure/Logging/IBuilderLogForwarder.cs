using UKHO.ADDS.EFS.Domain.Jobs;
using UKHO.ADDS.EFS.Domain.Products;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public interface IBuilderLogForwarder
    {
        Task ForwardLogsAsync(IEnumerable<string> messages, DataStandard dataStandard, JobId jobId);
    }
}

using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.VOS;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public interface IBuilderLogForwarder
    {
        Task ForwardLogsAsync(IEnumerable<string> messages, DataStandard dataStandard, JobId jobId);
    }
}

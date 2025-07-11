using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging
{
    public interface IBuilderLogForwarder
    {
        Task ForwardLogsAsync(IEnumerable<string> messages, DataStandard dataStandard, string jobId);
    }
}

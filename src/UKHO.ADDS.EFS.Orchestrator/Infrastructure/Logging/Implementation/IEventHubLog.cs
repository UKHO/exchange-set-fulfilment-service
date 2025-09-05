namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    public interface IEventHubLog : IDisposable
    {
        void Log(LogEntry logEntry);
    }
}

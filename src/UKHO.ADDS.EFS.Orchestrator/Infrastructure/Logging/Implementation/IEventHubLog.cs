using System;

namespace UKHO.Logging.EventHubLogProvider
{
    public interface IEventHubLog : IDisposable
    {
        void Log(LogEntry logEntry);
    }
}
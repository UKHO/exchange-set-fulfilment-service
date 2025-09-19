using Azure.Messaging.EventHubs;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    //Wrapper for the external library so we can test things.
    internal interface IEventHubClientWrapper : IDisposable
    {
        Task SendAsync(EventData eventData);

        void ValidateConnection();
    }
}

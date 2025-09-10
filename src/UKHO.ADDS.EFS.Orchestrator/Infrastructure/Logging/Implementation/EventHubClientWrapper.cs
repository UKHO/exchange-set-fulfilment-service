using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.Implementation
{
    //Wrapper for the external library so we can test things.
    internal interface IEventHubClientWrapper : IDisposable
    {
        Task SendAsync(EventData eventData);

        void ValidateConnection();
    }

    [ExcludeFromCodeCoverage] // not testable as it's just a wrapper for EventHubClient
    internal class EventHubClientWrapper : IEventHubClientWrapper
    {
        private EventHubProducerClient _eventHubClient;
        private bool _disposed;

        public EventHubClientWrapper(string fullyQualifiedNamespace, string eventHubName, TokenCredential credentials)
        {
            _eventHubClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credentials, clientOptions: new EventHubProducerClientOptions
            {
                ConnectionOptions = new EventHubConnectionOptions
                {
                    TransportType = EventHubsTransportType.AmqpWebSockets // supports firewall/proxy environments
                }
            });
        }

        public EventHubClientWrapper(string eventHubConnectionString, string eventHubEntityPath)
        {
            _eventHubClient = new EventHubProducerClient(eventHubConnectionString, eventHubEntityPath);
        }        

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }                

            if (disposing)
            {
                _eventHubClient?.DisposeAsync();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task SendAsync(EventData eventData)
        {
            return _eventHubClient.SendAsync(new List<EventData> { eventData });
        }

        public void ValidateConnection()
        {
            try
            {
                _eventHubClient.GetPartitionIdsAsync().Wait();
            }
            catch (AggregateException e)
            {
                throw new ArgumentException("The connection to EventHub failed.", e);
            }
        }
    }
}

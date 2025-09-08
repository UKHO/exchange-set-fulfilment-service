// British Crown Copyright © 2019,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

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
        private EventHubProducerClient eventHubClient;
        private bool _disposed;

        public EventHubClientWrapper(string fullyQualifiedNamespace,
                                     string eventHubName,
                                     TokenCredential credentials)
        {
            eventHubClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credentials, clientOptions: new EventHubProducerClientOptions
            {
                ConnectionOptions = new EventHubConnectionOptions
                {
                    TransportType = EventHubsTransportType.AmqpWebSockets // supports firewall/proxy environments
                }
            });
        }

        public EventHubClientWrapper(string eventHubConnectionString, string eventHubEntityPath)
        {
            eventHubClient = new EventHubProducerClient(eventHubConnectionString, eventHubEntityPath);
        }        

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                eventHubClient?.DisposeAsync();
                eventHubClient = null;
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
            return eventHubClient.SendAsync(new List<EventData> { eventData });
        }

        public void ValidateConnection()
        {
            try
            {
                eventHubClient.GetPartitionIdsAsync().Wait();
            }
            catch (AggregateException e)
            {
                throw new ArgumentException("The connection to EventHub failed.", e);
            }
        }
    }
}

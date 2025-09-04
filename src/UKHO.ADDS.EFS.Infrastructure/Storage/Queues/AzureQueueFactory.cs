using Azure.Storage.Queues;
using UKHO.ADDS.EFS.Domain.Services.Storage;

namespace UKHO.ADDS.EFS.Infrastructure.Storage.Queues
{
    internal sealed class AzureQueueFactory : IQueueFactory
    {
        private readonly QueueServiceClient _serviceClient;

        public AzureQueueFactory(QueueServiceClient serviceClient)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        }

        public IQueue GetQueue(string queueName)
        {
            var client = _serviceClient.GetQueueClient(queueName);
            return new AzureQueue(client);
        }
    }
}

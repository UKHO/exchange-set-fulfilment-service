using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace UKHO.ADDS.EFS.Infrastructure.Builders.Factories
{
    public interface IQueueClientFactory
    {
        QueueClient CreateRequestQueueClient(IConfiguration configuration);
        QueueClient CreateResponseQueueClient(IConfiguration configuration);
    }
}
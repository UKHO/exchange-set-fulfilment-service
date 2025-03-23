using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.EFS.Common.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Services.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddOrchestrator(this IServiceCollection collection, int queuePollingMaxMessages)
        {
            collection.AddSingleton(Channel.CreateBounded<ExchangeSetRequestMessage>(new BoundedChannelOptions(queuePollingMaxMessages)
            {
                FullMode = BoundedChannelFullMode.Wait
            }));

            collection.AddHostedService<QueuePollingService>();

            return collection;
        }
    }
}

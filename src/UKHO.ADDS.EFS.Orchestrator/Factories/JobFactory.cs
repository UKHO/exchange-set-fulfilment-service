using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Factories
{
    internal abstract class JobFactory<T> where T : ExchangeSetJob
    {
        public abstract Task CreateJobAsync(T job, CancellationToken stoppingToken);
    }
}

using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Services2.Jobs
{
    internal abstract class JobService
    {


        public abstract Task CreateJobAsync(ExchangeSetJob requestMessage, CancellationToken stoppingToken);

    }
}

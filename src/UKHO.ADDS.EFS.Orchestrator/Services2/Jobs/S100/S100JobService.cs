using UKHO.ADDS.EFS.Entities;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Services2.Jobs.S100
{
    internal class S100JobService : JobService
    {
        private readonly S100BuildRequestService _buildRequestService;

        public S100JobService(S100BuildRequestService buildRequestService)
        {
            _buildRequestService = buildRequestService;
        }

        public override async Task CreateJobAsync(ExchangeSetJob requestMessage, CancellationToken stoppingToken)
        {
            
        }
    }
}

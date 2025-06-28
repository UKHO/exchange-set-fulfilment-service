using UKHO.ADDS.EFS.Orchestrator.Builders.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors.S100
{
    internal class S100BuildResponseMonitor : BackgroundService
    {
        private readonly S100BuildResponseProcessor _buildResponseService;

        public S100BuildResponseMonitor(S100BuildResponseProcessor buildResponseService)
        {
            _buildResponseService = buildResponseService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            return Task.CompletedTask;
        }
    }
}

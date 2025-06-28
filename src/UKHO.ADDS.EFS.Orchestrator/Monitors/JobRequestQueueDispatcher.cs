using System.Threading.Channels;
using UKHO.ADDS.EFS.Jobs.S100;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Factories;
using UKHO.ADDS.EFS.Orchestrator.Factories.S100;

namespace UKHO.ADDS.EFS.Orchestrator.Monitors
{
    internal class JobRequestQueueDispatcher : BackgroundService
    {
        private readonly Channel<ExchangeSetRequestQueueMessage> _channel;
        private readonly ExchangeSetJobFactory _jobFactory;
        private readonly S100JobFactory _s100JobService;
        private readonly ILogger<JobRequestQueueDispatcher> _logger;

        public JobRequestQueueDispatcher(Channel<ExchangeSetRequestQueueMessage> channel, ExchangeSetJobFactory jobFactory, S100JobFactory s100JobService, ILogger<JobRequestQueueDispatcher> logger)
        {
            _channel = channel;
            _jobFactory = jobFactory;
            _s100JobService = s100JobService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var requestMessage in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                

                switch (requestMessage.DataStandard)
                {
                    case ExchangeSetDataStandard.S100:
                        var s100Job = await _jobFactory.CreateJob<S100ExchangeSetJob>(requestMessage);
                        await _s100JobService.CreateJobAsync(s100Job, stoppingToken);

                        break;
                    case ExchangeSetDataStandard.S63:
                    case ExchangeSetDataStandard.S57:
                    default:
                        break;
                }
            }
        }

    }
}

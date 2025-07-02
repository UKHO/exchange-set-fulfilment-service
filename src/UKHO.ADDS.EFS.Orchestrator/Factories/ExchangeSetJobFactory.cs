using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging.NewViews;

namespace UKHO.ADDS.EFS.Orchestrator.Factories
{
    internal class ExchangeSetJobFactory
    {
        private readonly ILogger<ExchangeSetJobFactory> _logger;

        public ExchangeSetJobFactory(ILogger<ExchangeSetJobFactory> logger)
        {
            _logger = logger;
        }

        public Task<T> CreateJob<T>(ExchangeSetRequestQueueMessage requestMessage) where T : ExchangeSetJob, new()
        {
            // The correlation ID becomes the job ID here
            var id = requestMessage.CorrelationId;

            var job = new T()
            {
                Id = id,
                DataStandard = requestMessage.DataStandard,
                Timestamp = DateTime.UtcNow,
                State = ExchangeSetJobState.Created,
            };

            _logger.LogJobCreated(requestMessage.CorrelationId, ExchangeSetJobLogView.Create(job));

            return Task.FromResult(job);
        }
    }
}

using Quartz;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.SchedulerJob
{
    public class EfsSchedulerJob : IJob
    {
        private readonly ILogger<EfsSchedulerJob> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;

        public EfsSchedulerJob(ILogger<EfsSchedulerJob> logger, IConfiguration config, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var pipelineFactory = _serviceProvider.GetRequiredService<AssemblyPipelineFactory>();

                var correlationId = $"job-{Guid.NewGuid():N}";

                _logger.LogEfsSchedulerJobStarted(correlationId, DateTime.UtcNow);

                var message = new JobRequestApiMessage
                {
                    Version = 1,
                    DataStandard = DataStandard.S100,
                    Products = "",
                    Filter = ""
                };

                var parameters = AssemblyPipelineParameters.CreateFrom(message, _config, correlationId);
                var pipeline = pipelineFactory.CreateAssemblyPipeline(parameters);

                var result = await pipeline.RunAsync(CancellationToken.None);
             
                _logger.LogEfsSchedulerJobCompleted(correlationId, result);
                
                _logger.LogEfsSchedulerJobNextRun(context.Trigger.GetNextFireTimeUtc()?.DateTime);
            }
            catch (Exception ex)
            {
                _logger.LogEfsSchedulerJobException(ex);
                throw;
            }
        }
    }
}

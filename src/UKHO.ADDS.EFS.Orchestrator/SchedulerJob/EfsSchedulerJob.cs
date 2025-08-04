using Quartz;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.SchedulerJob
{
    /// <summary>
    ///     Quartz.NET scheduled job that automatically triggers S100 exchange set generation
    ///     at configured intervals with comprehensive logging and error handling.
    /// </summary>
    [DisallowConcurrentExecution]
    public class EfsSchedulerJob : IJob
    {
        private readonly ILogger<EfsSchedulerJob> _logger;
        private readonly IConfiguration _config;
        private readonly IAssemblyPipelineFactory _pipelineFactory;

        public EfsSchedulerJob(ILogger<EfsSchedulerJob> logger, IConfiguration config, IAssemblyPipelineFactory pipelineFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        }

        /// <summary>
        ///     Executes scheduled S100 exchange set generation by creating and running an assembly pipeline.
        /// </summary>
        /// <param name="context">Quartz.NET job execution context containing trigger and scheduling information.</param>
        /// <returns>Task representing the asynchronous job execution.</returns>
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
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
                var pipeline = _pipelineFactory.CreateAssemblyPipeline(parameters);

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

using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal abstract class AssemblyPipeline
    {
        private readonly ILogger _logger;
        private readonly AssemblyPipelineNodeFactory _nodeFactory;
        private readonly AssemblyPipelineParameters _parameters;

        protected AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _logger = logger;
        }

        protected AssemblyPipelineParameters Parameters => _parameters;

        protected AssemblyPipelineNodeFactory NodeFactory => _nodeFactory;

        public abstract Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken);

        public T CreateJob<T>() where T : ExchangeSetJob, new()
        {
            var job = new T { Id = Parameters.JobId, DataStandard = Parameters.DataStandard, Timestamp = Parameters.Timestamp, State = ExchangeSetJobState.Created };

            _logger.LogJobCreated(ExchangeSetJobLogView.Create(job));

            return job;
        }
    }
}

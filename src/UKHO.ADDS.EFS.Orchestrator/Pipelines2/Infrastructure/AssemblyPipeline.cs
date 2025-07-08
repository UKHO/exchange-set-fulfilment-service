using UKHO.ADDS.EFS.NewEFS;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal abstract class AssemblyPipeline
    {
        public abstract Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken);
    }

    internal abstract class AssemblyPipeline<TBuild> : AssemblyPipeline where TBuild : Build
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

        protected abstract PipelineContext<TBuild> CreateContext();

        public Job CreateJob()
        {
            var job = new Job
            {
                Id = Parameters.JobId,
                DataStandard = Parameters.DataStandard,
                Timestamp = Parameters.Timestamp,
                JobState = JobState.Created,
                BuildState = BuildState.NotScheduled
            };

            _logger.LogJobCreated(EFSJobLogView.Create(job));

            return job;
        }
    }
}

using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Infrastructure.Logging;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal abstract class AssemblyPipeline
    {
        public abstract Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken);
    }

    internal abstract class AssemblyPipeline<TBuild> : AssemblyPipeline where TBuild : Build, new()
    {
        private readonly ILogger _logger;
        private readonly AssemblyPipelineNodeFactory _nodeFactory;
        private readonly PipelineContextFactory<TBuild> _contextFactory;
        private readonly AssemblyPipelineParameters _parameters;

        protected AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<TBuild> contextFactory, ILogger logger)
        {
            _parameters = parameters;
            _nodeFactory = nodeFactory;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        protected AssemblyPipelineParameters Parameters => _parameters;

        protected AssemblyPipelineNodeFactory NodeFactory => _nodeFactory;

        protected PipelineContextFactory<TBuild> ContextFactory => _contextFactory;

        protected abstract Task<PipelineContext<TBuild>> CreateContext();

        public Job CreateJob()
        {
            var job = new Job
            {
                Id = Parameters.JobId,
                DataStandard = Parameters.DataStandard,
                Timestamp = Parameters.Timestamp
            };

            _logger.LogJobCreated(EFSJobLogView.Create(job));

            return job;
        }
    }
}

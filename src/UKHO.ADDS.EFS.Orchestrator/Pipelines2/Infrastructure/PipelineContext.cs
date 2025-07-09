using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class PipelineContext<TBuild> where TBuild : Build
    {
        private readonly Job _job;
        private readonly TBuild _build;

        public PipelineContext(Job job, TBuild build)
        {
            _job = job;
            _build = build;
        }

        public Job Job => _job;

        public TBuild? Build => _build;
    }
}

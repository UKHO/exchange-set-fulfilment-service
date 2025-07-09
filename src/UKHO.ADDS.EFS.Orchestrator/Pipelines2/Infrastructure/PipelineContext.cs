using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Jobs;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Services;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure
{
    internal class PipelineContext<TBuild> where TBuild : Build
    {
        private readonly Job _job;
        private readonly TBuild _build;
        private readonly IStorageService _storageService;

        public PipelineContext(Job job, TBuild build, IStorageService storageService)
        {
            _job = job;
            _build = build;
            _storageService = storageService;
        }

        public Job Job => _job;

        public TBuild Build => _build;

        public async Task BuildRequired()
        {
            _job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task NoBuildRequired()
        {
            _job.ValidateAndSet(JobState.UpToDate, BuildState.None);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task Error()
        {
            _job.ValidateAndSet(JobState.Failed, BuildState.None);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task BuildScheduled()
        {
            _job.ValidateAndSet(JobState.Submitted, BuildState.Scheduled);
            await _storageService.UpdateJobAsync(_job);
        }
    }
}

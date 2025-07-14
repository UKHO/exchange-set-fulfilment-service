using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Orchestrator.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    partial class PipelineContext<TBuild> where TBuild : Build
    {
        public async Task SignalBuildRequired()
        {
            _job.ValidateAndSet(JobState.Created, BuildState.NotScheduled);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalNoBuildRequired()
        {
            _job.ValidateAndSet(JobState.UpToDate, BuildState.None);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalAssemblyError()
        {
            _job.ValidateAndSet(JobState.Failed, BuildState.None);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalBuildFailure()
        {
            _job.ValidateAndSet(JobState.Failed, BuildState.Failed);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalBuildScheduled()
        {
            _job.ValidateAndSet(JobState.Submitted, BuildState.Scheduled);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalCompleted()
        {
            _job.ValidateAndSet(JobState.Completed, BuildState.Succeeded);
            await _storageService.UpdateJobAsync(_job);
        }

        public async Task SignalCompletionFailure()
        {
            _job.ValidateAndSet(JobState.Failed, BuildState.Succeeded);
            await _storageService.UpdateJobAsync(_job);
        }
    }
}

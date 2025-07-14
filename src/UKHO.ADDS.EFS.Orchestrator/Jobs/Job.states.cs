using UKHO.ADDS.EFS.Builds;

namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    partial class Job
    {
        /// <summary>
        ///     Validate state transitions and set the job and build states accordingly
        /// </summary>
        /// <param name="jobState"></param>
        /// <param name="buildState"></param>
        internal void ValidateAndSet(JobState jobState, BuildState buildState)
        {
            // TODO Implement state validation

            JobState = jobState;
            BuildState = buildState;
        }
    }
}

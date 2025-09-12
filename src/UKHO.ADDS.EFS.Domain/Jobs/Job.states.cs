using UKHO.ADDS.EFS.Domain.Builds;

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    public partial class Job
    {
        /// <summary>
        ///     Validate state transitions and set the job and build states accordingly
        /// </summary>
        /// <param name="jobState"></param>
        /// <param name="buildState"></param>
        public void ValidateAndSet(JobState jobState, BuildState buildState)
        {
            // TODO Implement state validation

            JobState = jobState;
            BuildState = buildState;
        }
    }
}

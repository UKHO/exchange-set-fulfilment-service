namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    public enum JobState
    {
        Created,
        UpToDate,
        Duplicate,
        Submitted,
        Failed,
        Completed,
        CompletedWithError
    }
}

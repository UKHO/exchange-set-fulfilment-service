namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    internal enum JobTrigger
    {
        MarkUpToDate,
        MarkDuplicate,
        ScheduleBuild,
        AssemblyFailed,
        BuildFailed,
        Completed,
        CompletedWithError
    }
}

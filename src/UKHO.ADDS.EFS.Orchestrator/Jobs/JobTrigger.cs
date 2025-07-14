namespace UKHO.ADDS.EFS.Orchestrator.Jobs
{
    internal enum JobTrigger
    {
        MarkUpToDate,
        MarkDuplicate,
        CreateBuild,
        ScheduleBuild,
        AssemblyFailed,
        BuildFailed,
        Completed,
        CompletedWithError
    }
}

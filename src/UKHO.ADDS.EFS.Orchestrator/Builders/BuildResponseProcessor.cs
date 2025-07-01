using UKHO.ADDS.EFS.Builds;

namespace UKHO.ADDS.EFS.Orchestrator.Builders
{
    internal abstract class BuildResponseProcessor
    {
        public abstract Task ProcessBuildResponseAsync(BuildResponse buildResponse, BuildSummary buildSummary, CancellationToken stoppingToken);
    }
}

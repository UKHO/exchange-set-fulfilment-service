using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure
{
    internal class CompletionPipelineContext
    {
        public required string JobId { get; init; }

        public required BuilderExitCode ExitCode { get; init; }

        public required ExchangeSetDataStandard DataStandard { get; init; }

        public BuildSummary? BuildSummary { get; set; }
        public BuildStatus? BuildStatus { get; set; }
        public ExchangeSetJob? Job { get; set; }

        public static CompletionPipelineContext CreateFrom(BuildResponse buildResponse) =>
            new() { DataStandard = ExchangeSetDataStandard.S100, ExitCode = buildResponse.ExitCode, JobId = buildResponse.JobId };
    }
}

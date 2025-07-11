using UKHO.ADDS.EFS.Builds;
using UKHO.ADDS.EFS.Configuration.Orchestrator;
using UKHO.ADDS.EFS.Jobs;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Completion
{
    internal class CompletionPipelineParameters
    {
        public required DataStandard DataStandard { get; init; }

        public required string JobId { get; init; }

        public required BuilderExitCode ExitCode { get; init; }

        public static CompletionPipelineParameters CreateFrom(BuildResponse messageInstance, DataStandard dataStandard)
        {
            return new CompletionPipelineParameters() { DataStandard = dataStandard, JobId = messageInstance.JobId, ExitCode = messageInstance.ExitCode };
        }
    }
}

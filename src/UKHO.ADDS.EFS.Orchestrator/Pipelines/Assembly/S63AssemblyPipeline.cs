using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S63AssemblyPipeline : AssemblyPipeline
    {
        public S63AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, ILogger<S63AssemblyPipeline> logger)
            : base(parameters, nodeFactory, logger)
        {
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken) =>
            new() { JobId = Parameters.JobId, Status = NodeResultStatus.NotRun, DataStandard = Parameters.DataStandard, BatchId = string.Empty };
    }
}

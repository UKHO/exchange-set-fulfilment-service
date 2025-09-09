using UKHO.ADDS.EFS.Domain.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly;

internal class S100CustomAssemblyPipeline : AssemblyPipeline<S100Build>
{
    public S100CustomAssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S100Build> contextFactory, ILogger<S100CustomAssemblyPipeline> logger)
        : base(parameters, nodeFactory, contextFactory, logger)
    {
    }

    public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
    {
        var context = await CreateContext();

        AddPipelineNode<CreateJobNode>(cancellationToken);
        AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
        AddPipelineNode<CreateInputValidationNode>(cancellationToken);
        AddPipelineNode<newGetS100ProductNamesNode>(cancellationToken);
        AddPipelineNode<CheckFingerprintNode>(cancellationToken);
        AddPipelineNode<CreateFileShareBatchNode>(cancellationToken);
        AddPipelineNode<ScheduleBuildNode>(cancellationToken);
        AddPipelineNode<CreateFingerprintNode>(cancellationToken);
        AddPipelineNode<CreateResponseNode>(cancellationToken);

        var result = await Pipeline.ExecuteAsync(context);

        return new AssemblyPipelineResponse()
        {
            JobId = context.Job.Id,
            DataStandard = context.Job.DataStandard,
            JobStatus = context.Job.JobState,
            BuildStatus = context.Job.BuildState,
            BatchId = context.Job.BatchId,
            ErrorResponse = context.ErrorResponse?.Errors?.Count > 0 ? context.ErrorResponse : null,
            ResponseData = context.Build.ResponseData
        };
    }

    protected override async Task<PipelineContext<S100Build>> CreateContext()
    {
        return await ContextFactory.CreatePipelineContext(Parameters);
    }
}

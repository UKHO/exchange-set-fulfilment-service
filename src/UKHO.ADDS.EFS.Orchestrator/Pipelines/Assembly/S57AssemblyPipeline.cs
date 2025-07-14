using UKHO.ADDS.EFS.Builds.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly.Nodes.S57;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines.Infrastructure.Assembly;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines.Assembly
{
    internal class S57AssemblyPipeline : AssemblyPipeline<S57Build>
    {
        public S57AssemblyPipeline(AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S57Build> contextFactory, ILogger<S57AssemblyPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            AddPipelineNode<CreateJobNode>(cancellationToken);
            AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
            AddPipelineNode<GetProductsForDataStandardNode>(cancellationToken);
            AddPipelineNode<CreateFileShareBatchNode>(cancellationToken);
            AddPipelineNode<ScheduleBuildNode>(cancellationToken);

            var result = await Pipeline.ExecuteAsync(context);

            switch (result.Status)
            {
                case NodeResultStatus.Succeeded:
                case NodeResultStatus.SucceededWithErrors:
                    // Nothing to do here, the job is already updated in the context
                    break;

                case NodeResultStatus.NotRun:
                case NodeResultStatus.Failed:
                default:
                    await context.SignalAssemblyError();
                    break;
            }

            return new AssemblyPipelineResponse()
            {
                JobId = context.Job.Id,
                DataStandard = context.Job.DataStandard,
                JobStatus = context.Job.JobState,
                BuildStatus = context.Job.BuildState,
                BatchId = context.Job.BatchId
            };
        }

        protected override async Task<PipelineContext<S57Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}

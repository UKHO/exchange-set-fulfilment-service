using UKHO.ADDS.EFS.Builds.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly.Nodes.S100;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Infrastructure;
using UKHO.ADDS.EFS.Orchestrator.Pipelines2.Services;
using UKHO.ADDS.Infrastructure.Pipelines.Nodes;

namespace UKHO.ADDS.EFS.Orchestrator.Pipelines2.Assembly
{
    internal class S100AssemblyPipeline : AssemblyPipeline<S100Build>
    {
        private readonly IStorageService _storageService;

        public S100AssemblyPipeline(IStorageService storageService, AssemblyPipelineParameters parameters, AssemblyPipelineNodeFactory nodeFactory, PipelineContextFactory<S100Build> contextFactory, ILogger<S100AssemblyPipeline> logger)
            : base(parameters, nodeFactory, contextFactory, logger)
        {
            _storageService = storageService;
        }

        public override async Task<AssemblyPipelineResponse> RunAsync(CancellationToken cancellationToken)
        {
            var context = await CreateContext();

            await _storageService.CreateJobAsync(context.Job);

            AddPipelineNode<GetDataStandardTimestampNode>(cancellationToken);
            AddPipelineNode<GetProductsForDataStandardNode>(cancellationToken);
            AddPipelineNode<CreateFileShareBatchNode>(cancellationToken);

            var result = await Pipeline.ExecuteAsync(context);

            switch (result.Status)
            {
                case NodeResultStatus.Succeeded:
                case NodeResultStatus.SucceededWithErrors:
                    break;

                case NodeResultStatus.NotRun:
                case NodeResultStatus.Failed:
                default:
                    await context.Error();
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

        protected override async Task<PipelineContext<S100Build>> CreateContext()
        {
            return await ContextFactory.CreatePipelineContext(Parameters);
        }
    }
}
